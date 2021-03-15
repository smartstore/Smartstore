using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Security;
using Smartstore.Data;

namespace Smartstore.Web.Filters
{
    /// <summary>
    /// Checks whether the current user has the permission to access the shop.
    /// </summary>
    public sealed class AccessShopPermittedAttribute : TypeFilterAttribute
    {
        public AccessShopPermittedAttribute()
            : base(typeof(AccessShopFilter))
        {
        }

        class AccessShopFilter : IAsyncAuthorizationFilter
        {
            // TODO: (mg) (core) Check controller and action names when checking user's permission to access the shop.
            private static readonly List<Tuple<string, string>> s_permittedRoutes = new()
            {
                new Tuple<string, string>("Smartstore.Web.Controllers.CustomerController", "Login"),
                new Tuple<string, string>("Smartstore.Web.Controllers.CustomerController", "Logout"),
                new Tuple<string, string>("Smartstore.Web.Controllers.CustomerController", "Register"),
                new Tuple<string, string>("Smartstore.Web.Controllers.CustomerController", "PasswordRecovery"),
                new Tuple<string, string>("Smartstore.Web.Controllers.CustomerController", "PasswordRecoveryConfirm"),
                new Tuple<string, string>("Smartstore.Web.Controllers.CustomerController", "AccountActivation"),
                new Tuple<string, string>("Smartstore.Web.Controllers.CustomerController", "CheckUsernameAvailability"),
                new Tuple<string, string>("Smartstore.Web.Controllers.MenuController", "OffCanvas"),
                new Tuple<string, string>("Smartstore.Web.Controllers.ShoppingCartController", "CartSummary")
            };

            private readonly IPermissionService _permissionService;

            public AccessShopFilter(IPermissionService permissionService)
            {
                _permissionService = permissionService;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                Guard.NotNull(context, nameof(context));

                var request = context?.HttpContext?.Request;
                if (request == null)
                    return;

                var actionName = request.RouteValues.GetActionName();
                if (actionName.IsEmpty())
                    return;

                var controllerName = request.RouteValues.GetControllerName();
                if (controllerName.IsEmpty())
                    return;

                if (!DataSettings.DatabaseIsInstalled())
                    return;

                if (!IsPermittedRoute(controllerName, actionName) && !await HasStoreAccess())
                {
                    context.Result = new UnauthorizedResult();
                }
            }

            private async Task<bool> HasStoreAccess()
            {
                if (await _permissionService.AuthorizeAsync(Permissions.System.AccessShop))
                {
                    return true;
                }

                if (await _permissionService.AuthorizeByAliasAsync(Permissions.System.AccessShop))
                {
                    return true;
                }

                return false;
            }

            private static bool IsPermittedRoute(string controllerName, string actionName)
            {
                foreach (var route in s_permittedRoutes)
                {
                    if (controllerName.EqualsNoCase(route.Item1) && actionName.EqualsNoCase(route.Item2))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
