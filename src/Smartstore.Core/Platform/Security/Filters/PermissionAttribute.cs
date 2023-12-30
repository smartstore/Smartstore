using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Checks request permission for the current user.
    /// </summary>
    public partial class PermissionAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// E.g. [Permission(PermissionSystemNames.Customer.Read)]
        /// </summary>
        /// <param name="systemName">The system name of the permission.</param>
        /// <param name="showUnauthorizedMessage">A value indicating whether to show an unauthorization message.</param>
        public PermissionAttribute(string systemName, bool showUnauthorizedMessage = true)
            : base(typeof(PermissionFilter))
        {
            Guard.NotEmpty(systemName, nameof(systemName));

            Arguments = new[] { new PermissionRequirement(systemName, showUnauthorizedMessage) };
        }

        class PermissionFilter : IAsyncAuthorizationFilter
        {
            private readonly IPermissionService _permissionService;
            private readonly PermissionRequirement _requirement;

            public PermissionFilter(IPermissionService permissionService, PermissionRequirement requirement)
            {
                _permissionService = permissionService;
                _requirement = requirement;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                Guard.NotNull(context, nameof(context));

                if (await _permissionService.AuthorizeAsync(_requirement.SystemName))
                {
                    return;
                }

                await HandleUnauthorizedRequestAsync(context);
            }

            protected virtual async Task HandleUnauthorizedRequestAsync(AuthorizationFilterContext context)
            {
                var request = context.HttpContext.Request;

                var message = _requirement.ShowUnauthorizedMessage
                    ? await _permissionService.GetUnauthorizedMessageAsync(_requirement.SystemName)
                    : string.Empty;

                context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

                if (request.IsAjax())
                {
                    if (message.HasValue())
                    {
                        context.HttpContext.Response.Headers["X-Message-Type"] = "error";
                        context.HttpContext.Response.Headers["X-Message"] = Convert.ToBase64String(message.GetBytes());
                    }

                    var acceptTypes = request.Headers?.GetCommaSeparatedValues(HeaderNames.Accept);

                    if (acceptTypes?.Any(x => x.EqualsNoCase("text/html")) ?? false)
                    {
                        context.Result = new ContentResult
                        {
                            Content = message.HasValue() ? $"<div class=\"alert alert-danger\">{message}</div>" : "<div />",
                            ContentType = "text/html"
                        };
                    }
                    else
                    {
                        context.Result = new JsonResult(new
                        {
                            error = true,
                            success = false,
                            controller = request.RouteValues.GetControllerName(),
                            action = request.RouteValues.GetActionName()
                        });
                    }
                }
                else
                {
                    throw new AccessDeniedException(message);
                }
            }
        }
    }

    /// <summary>
    /// Required arguments to check permission by filter attribute.
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(string systemName, bool showUnauthorizedMessage)
        {
            SystemName = systemName;
            ShowUnauthorizedMessage = showUnauthorizedMessage;
        }

        /// <summary>
        /// The system name of the permission.
        /// </summary>
        public string SystemName { get; init; }

        /// <summary>
        /// A value indicating whether to show an unauthorization message.
        /// </summary>
        public bool ShowUnauthorizedMessage { get; init; }
    }
}
