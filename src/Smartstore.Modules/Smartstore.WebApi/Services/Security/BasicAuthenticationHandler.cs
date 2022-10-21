using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
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
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _apiService = apiService;
            _apiUserStore = apiUserStore;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var state = _apiService.GetState();

            try
            {
                if (!state.IsActive)
                {
                    throw new AuthenticationException(AccessDeniedReason.ApiDisabled);
                }

                if (!Request.IsHttps && Options.SslRequired)
                {
                    throw new AuthenticationException(AccessDeniedReason.SslRequired);
                }

                // INFO: must be executed before setting LastRequest.
                _apiUserStore.Activate(TimeSpan.FromMinutes(15));

                var user = await GetUser();
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.CustomerId.ToString(), ClaimValueTypes.Integer32, ClaimsIssuer)
                };

                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                user.LastRequest = DateTime.UtcNow;

                Request.HttpContext.Items["Smartstore.WebApi.MaxTop"] = state.MaxTop;
                Request.HttpContext.Items["Smartstore.WebApi.MaxExpansionDepth"] = state.MaxExpansionDepth;

                SetResponseHeaders(null, user, state);

                //$"Authenticated API request using scheme {Scheme.Name}: customer {user.CustomerId}.".Dump();
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                if (ex is AuthenticationException aex)
                {
                    Response.StatusCode = aex.StatusCode;
                }
                else
                {
                    Response.StatusCode = Status401Unauthorized;
                }

                Response.HttpContext.Features.Set<IExceptionHandlerPathFeature>(new AuthenticationExceptionPathFeature(ex, Request));

                SetResponseHeaders(ex, null, state);

                // TODO: (mg) (core) Continues with pipeline despite of the 401. Does not lead to a request rejection.
                // HandleChallengeAsync is never called! Investigate. Redirect required?
                return AuthenticateResult.Fail(ex);
            }
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (!Options.SuppressWWWAuthenticateHeader)
            {
                Response.Headers.WWWAuthenticate = AuthenticateHeader;
            }

            return Task.CompletedTask;
        }

        private async Task<WebApiUser> GetUser()
        {
            var rawAuthValue = Request?.Headers[HeaderNames.Authorization];
            if (!AuthenticationHeaderValue.TryParse(rawAuthValue, out var authHeader) || authHeader?.Parameter == null)
            {
                throw new AuthenticationException(AccessDeniedReason.InvalidAuthorizationHeader);
            }

            var credentialsStr = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
            if (!credentialsStr.SplitToPair(out var publicKey, out _, ":") || publicKey.IsEmpty())
            {
                throw new AuthenticationException(AccessDeniedReason.InvalidAuthorizationHeader);
            }

            var apiUsers = await _apiService.GetApiUsersAsync();
            if (!apiUsers.TryGetValue(publicKey, out var user) || user == null)
            {
                throw new AuthenticationException(AccessDeniedReason.UserUnknown, publicKey);
            }

            if (!user.Enabled)
            {
                throw new AuthenticationException(AccessDeniedReason.UserDisabled, publicKey);
            }

            if (credentialsStr != $"{user.PublicKey}:{user.SecretKey}")
            {
                throw new AuthenticationException(AccessDeniedReason.InvalidCredentials, publicKey);
            }

            return user;
        }

        private void SetResponseHeaders(Exception ex, WebApiUser user, WebApiState state)
        {
            var headers = Response.Headers;

            headers.Add(AppVersionHeader, SmartstoreVersion.CurrentFullVersion);
            headers.Add(VersionHeader, state.Version);
            headers.Add(MaxTopHeader, state.MaxTop.ToString());
            headers.Add(DateHeader, DateTime.UtcNow.ToString("o"));

            if (user != null)
            {
                headers.Add(CustomerIdHeader, user.CustomerId.ToString());
            }

            if (ex == null)
            {
                headers.CacheControl = "no-cache";
            }
            else
            {
                if (!Options.SuppressWWWAuthenticateHeader)
                {
                    headers.WWWAuthenticate = AuthenticateHeader;
                }

                if (ex is AuthenticationException aex)
                {
                    headers.Add(ResultIdHeader, ((int)aex.DeniedReason).ToString());
                    headers.Add(ResultDescriptionHeader, aex.DeniedReason.ToString());
                }
            }
        }
    }

    internal class AuthenticationExceptionPathFeature : IExceptionHandlerPathFeature
    {
        public AuthenticationExceptionPathFeature(Exception ex, HttpRequest request)
        {
            Error = ex;
            Path = request?.Path;
        }

        public Exception Error { get; }
        public string Path { get; }
    }
}
