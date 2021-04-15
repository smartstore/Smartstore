using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Data;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Checks whether the current user has the permission to access the administration backend.
    /// </summary>
    public sealed class AdminAuthorizeAttribute : TypeFilterAttribute
    {
        public AdminAuthorizeAttribute()
            : base(typeof(AdminAuthorizeFilter))
        {
        }

        class AdminAuthorizeFilter : IAsyncAuthorizationFilter
        {
            private readonly IPermissionService _permissionService;

            public AdminAuthorizeFilter(IPermissionService permissionService)
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

                if (context.Filters.Any(x => x is AdminAuthorizeFilter))
                {
                    if (!await HasAdminAccess())
                    {
                        context.Result = new ChallengeResult();
                    }
                }
            }

            private async Task<bool> HasAdminAccess()
            {
                if (await _permissionService.AuthorizeAsync(Permissions.System.AccessBackend))
                {
                    return true;
                }

                if (await _permissionService.AuthorizeByAliasAsync(Permissions.System.AccessBackend))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
