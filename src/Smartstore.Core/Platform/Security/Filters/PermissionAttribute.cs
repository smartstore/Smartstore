using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
            private readonly IWorkContext _workContext;
            private readonly Lazy<IUrlHelper> _urlHelper;
            private readonly Lazy<ITempDataDictionaryFactory> _tempDataDictionaryFactory;
            private readonly IPermissionService _permissionService;
            private readonly PermissionRequirement _requirement;

            public PermissionFilter(
                IWorkContext workContext,
                Lazy<IUrlHelper> urlHelper,
                Lazy<ITempDataDictionaryFactory> tempDataDictionaryFactory,
                IPermissionService permissionService, 
                PermissionRequirement requirement)
            {
                _workContext = workContext;
                _urlHelper = urlHelper;
                _tempDataDictionaryFactory = tempDataDictionaryFactory;
                _permissionService = permissionService;
                _requirement = requirement;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                Guard.NotNull(context, nameof(context));

                if (await _permissionService.AuthorizeAsync(_requirement.SystemName, _workContext.CurrentCustomer))
                {
                    return;
                }

                try
                {
                    await HandleUnauthorizedRequestAsync(context);
                }
                catch
                {
                    context.Result = new UnauthorizedResult();
                }                
            }

            protected virtual async Task HandleUnauthorizedRequestAsync(AuthorizationFilterContext context)
            {
                var request = context.HttpContext.Request;

                var message = _requirement.ShowUnauthorizedMessage
                    ? await _permissionService.GetUnauthorizedMessageAsync(_requirement.SystemName)
                    : string.Empty;

                if (request.IsAjaxRequest())
                {
                    if (message.HasValue())
                    {
                        context.HttpContext.Response.Headers.Add("X-Message-Type", "error");
                        context.HttpContext.Response.Headers.Add("X-Message", message);
                    }

                    var acceptTypes = request.Headers?.GetCommaSeparatedValues(HeaderNames.Accept);

                    if (acceptTypes?.Any(x => x.EqualsNoCase("text/html")) ?? false)
                    {
                        context.Result = AccessDeniedResult(message);
                    }
                    else
                    {
                        context.Result = new JsonResult(new
                        {
                            error = true,
                            success = false,
                            controller = request.RouteValues.GetControllerName(),
                            action = request.RouteValues.GetActionName()
                            //message
                        });
                    }
                }
                else
                {
                    // TODO: (mg) (core) The redirection in PermissionAttribute is bound to admin area, which makes the attribute
                    // usable in admin area only. Find a way to make this work for all areas.
                    // E.g.: throw an exception and let error handling middleware handle it (?)
                    var url = _urlHelper.Value.Action("AccessDenied", "Security", new 
                    {
                        permission = _requirement.SystemName,
                        pageUrl = request.RawUrl(), 
                        area = "admin"
                    });

                    // TODO: (core) ITempDataDictionaryFactory doesn't work in PermissionAttribute. Nothing arrives in an action method.
                    var tempData = _tempDataDictionaryFactory.Value.GetTempData(context.HttpContext);
                    tempData["UnauthorizedMessage"] = message;

                    context.Result = new RedirectResult(url);
                }
            }

            protected virtual ActionResult AccessDeniedResult(string message)
            {
                return new ContentResult
                {
                    Content = message.HasValue() ? $"<div class=\"alert alert-danger\">{message}</div>" : string.Empty,
                    ContentType = "text/html"
                };
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
