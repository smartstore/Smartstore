using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Checks request permission for the current user.
    /// </summary>
    public class PermissionAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// e.g. [Permission(PermissionSystemNames.Customer.Read)]
        /// </summary>
        /// <param name="systemName">The system name of the permission.</param>
        /// <param name="showUnauthorizedMessage">Whether to show an unauthorization message.</param>
        public PermissionAttribute(string systemName, bool showUnauthorizedMessage = true) 
            : base(typeof(PermissionFilter))
        {
            Arguments = new object[] { systemName, showUnauthorizedMessage };

            SystemName = systemName;
            ShowUnauthorizedMessage = showUnauthorizedMessage;
        }

        /// <summary>
        /// The system name of the permission.
        /// </summary>
        public string SystemName { get; }

        /// <summary>
        /// Whether to show an unauthorization message.
        /// </summary>
        public bool ShowUnauthorizedMessage { get; }

        class PermissionFilter : IAsyncAuthorizationFilter
        {
            private readonly IWorkContext _workContext;
            private readonly IPermissionService _permissionService;

            public PermissionFilter(IWorkContext workContext, IPermissionService permissionService)
            {
                _workContext = workContext;
                _permissionService = permissionService;
            }

            public Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                // TODO: (core) Implement PermissionFilter
                return Task.CompletedTask;
            }
        }
    }
}
