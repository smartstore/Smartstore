using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Checks whether the current user has the permission to access the administration backend.
    /// </summary>
    public sealed class AuthorizeAdminAttribute : TypeFilterAttribute
    {
        public AuthorizeAdminAttribute()
            : base(typeof(AuthorizeAdminFilter))
        {
        }

        class AuthorizeAdminFilter : IAsyncAuthorizationFilter
        {
            private readonly IPermissionService _permissionService;

            public AuthorizeAdminFilter(IPermissionService permissionService)
            {
                _permissionService = permissionService;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                Guard.NotNull(context, nameof(context));

                var overrideFilter = context.ActionDescriptor.FilterDescriptors
                    .Where(x => x.Scope == FilterScope.Action)
                    .Select(x => x.Filter)
                    .OfType<NeverAuthorizeAttribute>()
                    .FirstOrDefault();

                if (overrideFilter != null)
                {
                    return;
                }

                if (context.Filters.Any(x => x is AuthorizeAdminFilter))
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

                return false;
            }
        }
    }
}
