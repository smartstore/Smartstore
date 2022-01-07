using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Data;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Checks whether the current user has the permission to access the shop.
    /// </summary>
    public sealed class AuthorizeShopAccessAttribute : TypeFilterAttribute
    {
        public AuthorizeShopAccessAttribute()
            : base(typeof(AuthorizeShopAccessFilter))
        {
        }

        class AuthorizeShopAccessFilter : IAsyncAuthorizationFilter
        {
            private readonly IPermissionService _permissionService;

            public AuthorizeShopAccessFilter(IPermissionService permissionService)
            {
                _permissionService = permissionService;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                Guard.NotNull(context, nameof(context));

                if (!DataSettings.DatabaseIsInstalled())
                {
                    return;
                }

                var overrideFilter = context.ActionDescriptor.FilterDescriptors
                    .Where(x => x.Scope == FilterScope.Action)
                    .Select(x => x.Filter)
                    .OfType<NeverAuthorizeAttribute>()
                    .FirstOrDefault();

                if (overrideFilter != null)
                {
                    return;
                }

                if (!await HasStoreAccess())
                {
                    context.Result = new ChallengeResult();
                }
            }

            private async Task<bool> HasStoreAccess()
            {
                if (await _permissionService.AuthorizeAsync(Permissions.System.AccessShop))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
