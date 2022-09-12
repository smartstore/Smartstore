using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.WebApi.Models;

namespace Smartstore.WebApi.Filters
{
    // TODO: (mg) (core) SSL mandatory but not for developers!
    /// <summary>
    /// Verifies the identity of a user using basic authentication.
    /// Also checks whether the user has a certain permission to a Web API resource.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class WebApiAuthorizeAttribute : TypeFilterAttribute
    {
        public WebApiAuthorizeAttribute()
            : this(null)
        {
        }

        public WebApiAuthorizeAttribute(string permission)
            : base(typeof(WebApiAuthorizeFilter))
        {
            Permission = permission;
            Arguments = new object[] { this };
        }

        /// <summary>
        /// The system name of the permission.
        /// </summary>
        public string Permission { get; }

        class WebApiAuthorizeFilter : IAsyncAuthorizationFilter
        {
            private readonly WebApiAuthorizeAttribute _attribute;
            private readonly SmartDbContext _db;
            private readonly IPermissionService _permissionService;
            private readonly IWebApiService _apiService;

            public WebApiAuthorizeFilter(
                WebApiAuthorizeAttribute attribute,
                SmartDbContext db,
                IPermissionService permissionService,
                IWebApiService apiService)
            {
                _attribute = attribute;
                _db = db;
                _permissionService = permissionService;
                _apiService = apiService;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                //var user = (User)context.HttpContext.Items["User"];.....
                //if (credentials.SplitToPair(out var userName, out var password, ":"))
                //{
                //    context.Items["User"] = await userService.Authenticate(username, password);
                //}

                try
                {
                    var (customer, user) = await GetAuthorizedCustomer(context);
                    if (customer != null && user != null)
                    {
                        //IdentityOptions.ClaimsIdentity.UserIdClaimType
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                            new Claim(ClaimTypes.Name, customer.Username),
                        };

                        context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));
                    }
                    else
                    {
                        //context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                        //context.HttpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"\", charset=\"UTF-8\"";
                    }
                }
                catch (WebApiAuthorizationException)
                {
                    // TODO
                }
                catch (Exception)
                {
                    // TODO
                }
            }

            private async Task<(Customer Customer, WebApiUser User)> GetAuthorizedCustomer(AuthorizationFilterContext ctx)
            {
                var rawAuthValue = ctx?.HttpContext?.Request?.Headers["Authorization"];
                if (!AuthenticationHeaderValue.TryParse(rawAuthValue, out var authHeader) || authHeader?.Parameter == null)
                {
                    throw new WebApiAuthorizationException(AccessDeniedReason.InvalidAuthorizationHeader);
                }

                var credentialsStr = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
                if (!credentialsStr.SplitToPair(out var publicKey, out _, ":") || publicKey.IsEmpty())
                {
                    throw new WebApiAuthorizationException(AccessDeniedReason.InvalidAuthorizationHeader);
                }

                var apiUsers = await _apiService.GetApiUsersAsync();
                if (!apiUsers.TryGetValue(publicKey, out var user) || user == null)
                {
                    throw new WebApiAuthorizationException(AccessDeniedReason.UserUnknown, publicKey);
                }

                if (!user.Enabled)
                {
                    throw new WebApiAuthorizationException(AccessDeniedReason.UserDisabled, publicKey);
                }

                if (credentialsStr != $"{user.PublicKey}:{user.SecretKey}")
                {
                    throw new WebApiAuthorizationException(AccessDeniedReason.InvalidCredentials, publicKey);
                }

                var customer = await _db.Customers
                    .IncludeCustomerRoles()
                    .FindByIdAsync(user.CustomerId, false);

                if (customer == null)
                {
                    throw new WebApiAuthorizationException(AccessDeniedReason.UserUnknown, publicKey);
                }

                if (!customer.Active)
                {
                    throw new WebApiAuthorizationException(AccessDeniedReason.UserInactive, publicKey);
                }

                if (_attribute.Permission.HasValue() && !await _permissionService.AuthorizeAsync(_attribute.Permission, customer))
                {
                    throw new WebApiAuthorizationException(AccessDeniedReason.UserHasNoPermission, publicKey, _attribute.Permission);
                }

                user.LastRequest = DateTime.UtcNow;
                return (customer, user);
            }
        }
    }
}
