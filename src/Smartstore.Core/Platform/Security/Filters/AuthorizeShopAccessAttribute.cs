using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
            : base(typeof(AccessShopFilter))
        {
        }

        class AccessShopFilter : IAsyncAuthorizationFilter
        {
            private readonly IPermissionService _permissionService;

            public AccessShopFilter(IPermissionService permissionService)
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

                var endpoint = context?.HttpContext?.GetEndpoint();
                if (endpoint?.Metadata?.GetMetadata<NeverAuthorizeAttribute>() != null)
                {
                    return;
                }

                if (!await HasStoreAccess())
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
        }
    }
}
