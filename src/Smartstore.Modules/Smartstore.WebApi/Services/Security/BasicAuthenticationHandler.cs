using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OData;

namespace Smartstore.Web.Api.Security
{
    /// <summary>
    /// Verifies the identity of a user using basic authentication.
    /// Also ensures that requests are sent via HTTPS (except in a development environment).
    /// </summary>
    public sealed class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
    {
        const string AuthenticateHeader = "Basic realm=\"Smartstore.WebApi\", charset=\"UTF-8\"";
        internal const string AppVersionHeader = "Smartstore-Api-AppVersion";
        internal const string VersionHeader = "Smartstore-Api-Version";
        internal const string MaxTopHeader = "Smartstore-Api-MaxTop";
        internal const string DateHeader = "Smartstore-Api-Date";
        internal const string CustomerIdHeader = "Smartstore-Api-CustomerId";
        internal const string ResultIdHeader = "Smartstore-Api-AuthResultId";
        internal const string ResultDescriptionHeader = "Smartstore-Api-AuthResultDesc";

        private readonly IWebApiService _apiService;
        private readonly IApiUserStore _apiUserStore;

        public BasicAuthenticationHandler(
            IWebApiService apiService,
            IApiUserStore apiUserStore,
            IOptionsMonitor<BasicAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
            _apiService = apiService;
            _apiUserStore = apiUserStore;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var state = _apiService.GetState();

            try
            {
                // INFO: for batch requests, these headers are only present in the first response.
                var headers = Response.Headers;
                headers[AppVersionHeader] = SmartstoreVersion.CurrentFullVersion;
                headers[VersionHeader] = state.Version;
                headers[MaxTopHeader] = state.MaxTop.ToString();
                headers[DateHeader] = DateTime.UtcNow.ToString("o");

                if (!state.IsActive)
                {
                    return Failure(AccessDeniedReason.ApiDisabled);
                }

                if (!Request.IsHttps && Options.SslRequired)
                {
                    return Failure(AccessDeniedReason.SslRequired, null, Status421MisdirectedRequest);
                }

                // INFO: must be executed before setting LastRequest.
                _apiUserStore.Activate(TimeSpan.FromMinutes(15));

                var rawAuthValue = Request?.Headers[HeaderNames.Authorization];
                if (!AuthenticationHeaderValue.TryParse(rawAuthValue, out var authHeader) || authHeader?.Parameter == null)
                {
                    return Failure(AccessDeniedReason.InvalidAuthorizationHeader);
                }

                var credentialsStr = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
                if (!credentialsStr.SplitToPair(out var publicKey, out _, ":") || publicKey.IsEmpty())
                {
                    return Failure(AccessDeniedReason.InvalidAuthorizationHeader);
                }

                var apiUsers = await _apiService.GetApiUsersAsync();
                if (!apiUsers.TryGetValue(publicKey, out var user) || user == null)
                {
                    return Failure(AccessDeniedReason.UserUnknown, publicKey);
                }

                if (!user.Enabled)
                {
                    return Failure(AccessDeniedReason.UserDisabled, publicKey);
                }

                if (credentialsStr != $"{user.PublicKey}:{user.SecretKey}")
                {
                    return Failure(AccessDeniedReason.InvalidCredentials, publicKey);
                }

                user.LastRequest = DateTime.UtcNow;

                Request.HttpContext.Items[MaxApiQueryOptions.Key] = new MaxApiQueryOptions
                {
                    MaxTop = state.MaxTop,
                    MaxExpansionDepth = state.MaxExpansionDepth
                };

                headers[CustomerIdHeader] = user.CustomerId.ToString();
                headers.CacheControl = "no-cache";

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.CustomerId.ToString(), ClaimValueTypes.Integer32, ClaimsIssuer)
                };

                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                //$"Authenticated API request using scheme {Scheme.Name}: customer {user.CustomerId}.".Dump();
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                return Failure(ex.Message, ex);
            }
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // INFO: status code and response headers already set in HandleAuthenticateAsync.
            return Task.CompletedTask;
        }

        private AuthenticateResult Failure(AccessDeniedReason reason, string publicKey = null, int statusCode = Status401Unauthorized)
        {
            Response.Headers[ResultIdHeader] = ((int)reason).ToString();
            Response.Headers[ResultDescriptionHeader] = reason.ToString();

            return Failure(CreateMessage(reason, publicKey), null, statusCode);
        }

        private AuthenticateResult Failure(string message, Exception ex = null, int statusCode = Status401Unauthorized)
        {
            Response.StatusCode = statusCode;

            if (!Options.SuppressWWWAuthenticateHeader)
            {
                Response.Headers.WWWAuthenticate = AuthenticateHeader;
            }

            var odataError = new ODataError
            {
                ErrorCode = statusCode.ToString(),
                Message = message,
                InnerError = ex != null ? new ODataInnerError(ex) : null
            };

            var odataEx = new ODataErrorException(message, ex, odataError);
            odataEx.Data["JsonContent"] = odataError.ToString();

            // Let the ErrorController handle ODataErrorException.
            Response.HttpContext.Features.Set<IExceptionHandlerPathFeature>(new ODataExceptionHandlerPathFeature(odataEx, Request));

            return AuthenticateResult.Fail(odataEx);
        }

        private string CreateMessage(AccessDeniedReason reason, string publicKey)
        {
            string msg = null;

            switch (reason)
            {
                case AccessDeniedReason.ApiDisabled:
                    msg = "Web API is disabled.";
                    break;
                case AccessDeniedReason.SslRequired:
                    msg = "Web API requests require SSL.";
                    break;
                case AccessDeniedReason.InvalidAuthorizationHeader:
                    msg = "Missing or invalid authorization header. Must have the format 'PublicKey:SecretKey'.";
                    break;
                case AccessDeniedReason.InvalidCredentials:
                    msg = $"The credentials sent for user with public key {publicKey.NaIfEmpty()} do not match.";
                    break;
                case AccessDeniedReason.UserUnknown:
                    msg = $"Unknown user. The public key {publicKey.NaIfEmpty()} does not exist.";
                    break;
                case AccessDeniedReason.UserDisabled:
                    msg = $"Access via Web API is disabled for the user with public key {publicKey.NaIfEmpty()}.";
                    break;
            }

            msg = msg.Grow(Request.UserAgent(), " User agent: ");

            return $"Access to the Web API was denied. Reason: {reason}. {msg}";
        }
    }
}
