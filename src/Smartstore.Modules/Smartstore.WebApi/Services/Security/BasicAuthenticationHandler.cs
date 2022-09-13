using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Seo;
using Smartstore.Utilities;
using Smartstore.WebApi.Models;

namespace Smartstore.WebApi.Services
{
    /// <summary>
    /// Verifies the identity of a user using basic authentication.
    /// Also ensures that requests are sent via HTTPS.
    /// </summary>
    public sealed class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly SmartDbContext _db;
        private readonly IWebApiService _apiService;
        private readonly SignInManager<Customer> _signInManager;
        private readonly Lazy<IUrlService> _urlService;

        public BasicAuthenticationHandler(
            SmartDbContext db,
            IWebApiService apiService,
            SignInManager<Customer> signInManager,
            Lazy<IUrlService> urlService,
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _db = db;
            _apiService = apiService;
            _signInManager = signInManager;
            _urlService = urlService;
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

                if (!Request.Scheme.EqualsNoCase(Uri.UriSchemeHttps) && !CommonHelper.IsDevEnvironment)
                {
                    throw new AuthenticationException(AccessDeniedReason.SslRequired);
                }

                var (customer, user) = await GetCustomer();

                await _signInManager.SignInAsync(customer, true);
                //$"Signed in using '{Scheme.Name}'.".Dump();

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                    new Claim(ClaimTypes.Name, customer.Username)
                };

                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                SetResponseHeaders(null, customer, state);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                Response.HttpContext.Features.Set<IExceptionHandlerPathFeature>(new AuthenticationExceptionPathFeature(ex, Request));

                var policy = _urlService.Value.GetUrlPolicy();
                if (policy?.Endpoint == null)
                {
                    policy.Endpoint = Request.HttpContext.GetEndpoint();
                }

                SetResponseHeaders(ex, null, state);

                return AuthenticateResult.Fail(ex);
            }
        }

        private async Task<(Customer Customer, WebApiUser User)> GetCustomer()
        {
            var rawAuthValue = Request?.Headers["Authorization"];
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

            var customer = await _db.Customers.FindByIdAsync(user.CustomerId, false);
            if (customer == null)
            {
                throw new AuthenticationException(AccessDeniedReason.UserUnknown, publicKey);
            }

            if (!customer.Active)
            {
                throw new AuthenticationException(AccessDeniedReason.UserInactive, publicKey);
            }

            user.LastRequest = DateTime.UtcNow;
            return (customer, user);
        }

        private void SetResponseHeaders(Exception ex, Customer customer, WebApiState state)
        {
            var headers = Response.Headers;

            headers.Add("Smartstore-Api-AppVersion", SmartstoreVersion.CurrentFullVersion);
            headers.Add("Smartstore-Api-Version", state.Version);
            headers.Add("Smartstore-Api-MaxTop", state.MaxTop.ToString());
            headers.Add("Smartstore-Api-Date", DateTime.UtcNow.ToString("o"));

            if (customer != null)
            {
                headers.Add("Smartstore-Api-CustomerId", customer.Id.ToString());
            }

            if (ex == null)
            {
                headers.CacheControl = "no-cache";
            }
            else
            {
                headers.WWWAuthenticate = $"Basic realm=\"{Module.SystemName}\", charset=\"UTF-8\"";

                if (ex is AuthenticationException authEx)
                {
                    headers.Add("Smartstore-Api-AuthResultId", ((int)authEx.DeniedReason).ToString());
                    headers.Add("Smartstore-Api-AuthResultDesc", authEx.DeniedReason.ToString());
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





    // TODO: (mg) (core) SSL mandatory but not for developers!
    /// <summary>
    /// Verifies the identity of a user using basic authentication.
    /// Also checks whether the user has a certain permission to a Web API resource.
    /// </summary>
    //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    //public sealed class WebApiAuthorizeAttribute : TypeFilterAttribute
    //{
    //    public WebApiAuthorizeAttribute()
    //        : this(null)
    //    {
    //    }

    //    public WebApiAuthorizeAttribute(string permission)
    //        : base(typeof(WebApiAuthorizeFilter))
    //    {
    //        Permission = permission;
    //        Arguments = new object[] { this };
    //    }

    //    /// <summary>
    //    /// The system name of the permission.
    //    /// </summary>
    //    public string Permission { get; }

    //    class WebApiAuthorizeFilter : IAsyncAuthorizationFilter
    //    {
    //        private readonly WebApiAuthorizeAttribute _attribute;
    //        private readonly SmartDbContext _db;
    //        private readonly IPermissionService _permissionService;
    //        private readonly IWebApiService _apiService;
    //        private readonly SignInManager<Customer> _signInManager;

    //        public WebApiAuthorizeFilter(
    //            WebApiAuthorizeAttribute attribute,
    //            SmartDbContext db,
    //            IPermissionService permissionService,
    //            IWebApiService apiService,
    //            SignInManager<Customer> signInManager)
    //        {
    //            _attribute = attribute;
    //            _db = db;
    //            _permissionService = permissionService;
    //            _apiService = apiService;
    //            _signInManager = signInManager;
    //        }

    //        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    //        {
    //            //var user = (User)context.HttpContext.Items["User"];.....
    //            //if (credentials.SplitToPair(out var userName, out var password, ":"))
    //            //{
    //            //    context.Items["User"] = await userService.Authenticate(username, password);
    //            //}

    //            try
    //            {
    //                var (customer, user) = await GetAuthorizedCustomer(context);

    //                var claims = new[]
    //                {
    //                    new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
    //                    new Claim(ClaimTypes.Name, customer.Username)
    //                };

    //                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));

    //                //context.HttpContext.User = principal;
    //                //var authResult = await context.HttpContext.AuthenticateAsync(); // no luck here
    //                //$"authResult {authResult.Succeeded}".Dump();

    //                //var authResult2 = await context.HttpContext.AuthenticateAsync("Basic"); // had to register.... wrong way?
    //                //$"authResult2 {authResult2.Succeeded}".Dump();

    //                //var ticket = new AuthenticationTicket(principal, "Basic");
    //                //await context.HttpContext.SignInAsync(principal);

    //                await _signInManager.SignInAsync(customer, true);
    //            }
    //            catch (WebApiAuthorizationException)
    //            {
    //                // TODO
    //                //context.HttpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"\", charset=\"UTF-8\"";
    //            }
    //            catch (Exception ex)
    //            {
    //                // TODO
    //                ex.Dump();
    //            }
    //        }

    //        private async Task<(Customer Customer, WebApiUser User)> GetAuthorizedCustomer(AuthorizationFilterContext ctx)
    //        {
    //            var rawAuthValue = ctx?.HttpContext?.Request?.Headers["Authorization"];
    //            if (!AuthenticationHeaderValue.TryParse(rawAuthValue, out var authHeader) || authHeader?.Parameter == null)
    //            {
    //                throw new WebApiAuthorizationException(AccessDeniedReason.InvalidAuthorizationHeader);
    //            }

    //            var credentialsStr = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
    //            if (!credentialsStr.SplitToPair(out var publicKey, out _, ":") || publicKey.IsEmpty())
    //            {
    //                throw new WebApiAuthorizationException(AccessDeniedReason.InvalidAuthorizationHeader);
    //            }

    //            var apiUsers = await _apiService.GetApiUsersAsync();
    //            if (!apiUsers.TryGetValue(publicKey, out var user) || user == null)
    //            {
    //                throw new WebApiAuthorizationException(AccessDeniedReason.UserUnknown, publicKey);
    //            }

    //            if (!user.Enabled)
    //            {
    //                throw new WebApiAuthorizationException(AccessDeniedReason.UserDisabled, publicKey);
    //            }

    //            if (credentialsStr != $"{user.PublicKey}:{user.SecretKey}")
    //            {
    //                throw new WebApiAuthorizationException(AccessDeniedReason.InvalidCredentials, publicKey);
    //            }

    //            var customer = await _db.Customers
    //                .IncludeCustomerRoles()
    //                .FindByIdAsync(user.CustomerId, false);

    //            if (customer == null)
    //            {
    //                throw new WebApiAuthorizationException(AccessDeniedReason.UserUnknown, publicKey);
    //            }

    //            if (!customer.Active)
    //            {
    //                throw new WebApiAuthorizationException(AccessDeniedReason.UserInactive, publicKey);
    //            }

    //            if (_attribute.Permission.HasValue() && !await _permissionService.AuthorizeAsync(_attribute.Permission, customer))
    //            {
    //                throw new WebApiAuthorizationException(AccessDeniedReason.UserHasNoPermission, publicKey, _attribute.Permission);
    //            }

    //            user.LastRequest = DateTime.UtcNow;
    //            return (customer, user);
    //        }
    //    }
    //}
}
