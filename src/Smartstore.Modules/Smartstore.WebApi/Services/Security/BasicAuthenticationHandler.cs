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
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api.Security
{
    /// <summary>
    /// Verifies the identity of a user using basic authentication.
    /// Also ensures that requests are sent via HTTPS (except in a development environment).
    /// </summary>
    public sealed class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
    {
        const string AuthenticateHeader = "Basic realm=\"Smartstore.WebApi\", charset=\"UTF-8\"";
        const string AppVersionHeader = "Smartstore-Api-AppVersion";
        const string VersionHeader = "Smartstore-Api-Version";
        const string MaxTopHeader = "Smartstore-Api-MaxTop";
        const string DateHeader = "Smartstore-Api-Date";
        const string CustomerIdHeader = "Smartstore-Api-CustomerId";
        const string ResultIdHeader = "Smartstore-Api-AuthResultId";
        const string ResultDescriptionHeader = "Smartstore-Api-AuthResultDesc";

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
                    return Failure(AccessDeniedReason.SslRequired, null, null, Status421MisdirectedRequest);
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
                    return Failure(AccessDeniedReason.UserUnknown, user, publicKey);
                }

                if (!user.Enabled)
                {
                    return Failure(AccessDeniedReason.UserDisabled, user, publicKey);
                }

                if (credentialsStr != $"{user.PublicKey}:{user.SecretKey}")
                {
                    return Failure(AccessDeniedReason.InvalidCredentials, user, publicKey);
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

        private AuthenticateResult Failure(
            AccessDeniedReason reason,
            WebApiUser user = null,
            string publicKey = null, 
            int statusCode = Status401Unauthorized)
        {
            var reasonId = ((int)reason).ToString();
            var reasonDescription = reason.ToString();
            var msg = $"Access to the Web API was denied ({reason}). ";

            Response.Headers[ResultIdHeader] = reasonId;
            Response.Headers[ResultDescriptionHeader] = reasonDescription;

            switch (reason)
            {
                case AccessDeniedReason.ApiDisabled:
                    msg += "Web API is disabled.";
                    break;
                case AccessDeniedReason.SslRequired:
                    msg += "Web API requests require SSL.";
                    break;
                case AccessDeniedReason.InvalidAuthorizationHeader:
                    msg += "Missing or invalid authorization header. Must have the format 'PublicKey:SecretKey'.";
                    break;
                case AccessDeniedReason.InvalidCredentials:
                    msg += $"The credentials sent for user with public key {publicKey.NaIfEmpty()} do not match.";
                    break;
                case AccessDeniedReason.UserUnknown:
                    msg += $"The user is unknown. The public key {publicKey.NaIfEmpty()} does not exist.";
                    break;
                case AccessDeniedReason.UserDisabled:
                    msg += $"Access via Web API is disabled for the user with public key {publicKey.NaIfEmpty()}.";
                    break;
            }

            var details = new List<ODataErrorDetail>
            {
                new() { Target = "User agent", Message = Request.UserAgent() },
                new() { Code = reasonId, Target = "Reason", Message = reasonDescription },
            };

            if (publicKey.HasValue())
            {
                details.Add(new() { Target = "Public key", Message = publicKey });
            }

            if (user != null)
            {
                details.Add(new() { Target = "Customer ID", Message = user.CustomerId.ToString() });
            }

            return Failure(msg, null, statusCode, details);
        }

        private AuthenticateResult Failure(
            string message,
            Exception ex = null, 
            int statusCode = Status401Unauthorized,
            ICollection<ODataErrorDetail> details = null)
        {
            Response.StatusCode = statusCode;

            if (!Options.SuppressWWWAuthenticateHeader)
            {
                Response.Headers.WWWAuthenticate = AuthenticateHeader;
            }

            var error = ODataHelper.CreateError(message, statusCode, ex, details);
            var odataEx = new ODataErrorException(message, ex, error);
            odataEx.Data["JsonContent"] = error.ToString();

            // Let the ErrorController handle ODataErrorException.
            Response.HttpContext.Features.Set<IExceptionHandlerPathFeature>(new ODataExceptionHandlerPathFeature(odataEx, Request));

            return AuthenticateResult.Fail(odataEx);
        }
    }
}
