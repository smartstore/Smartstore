using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Content.Menus
{
    public class MenuAttribute : TypeFilterAttribute
    {
        public MenuAttribute()
            : base(typeof(MenuFilter))
        {
        }

        // TODO: (mh) (core): This is only a mock. Remove & replace once properties are available.
        class WebViewPageHelper
        {
            public static string CurrentPageType => "";
            public static object CurrentPageId => 0;
            public static bool IsHomePage => false;
        }

        class MenuFilter : IAsyncActionFilter, IAsyncResultFilter
        {
            private readonly IMenuStorage _menuStorage;
            private readonly IMenuService _menuService;
            private readonly IWidgetProvider _widgetProvider;
            private readonly IHtmlHelper _htmlHelper;

            public MenuFilter(
                IMenuStorage menuStorage,
                IMenuService menuService,
                IWidgetProvider widgetProvider,
                IHtmlHelper htmlHelper)
            {
                _menuStorage = menuStorage;
                _menuService = menuService;
                _widgetProvider = widgetProvider;
                _htmlHelper = htmlHelper;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (context.HttpContext.Request.IsAjaxRequest())
                    return;

                //if (filterContext.HttpContext.Request.HttpMethod != "GET")
                //	return;

                if (context.Result?.IsHtmlViewResult() == false)
                    return;

                if (context.RouteData.Values.GetAreaName().EqualsNoCase("admin"))
                    return;

                var selectedNode = await ResolveCurrentNodeAsync(context);

                object nodeData;

                if (selectedNode == null)
                {
                    nodeData = new
                    {
                        type = WebViewPageHelper.CurrentPageType,
                        id = WebViewPageHelper.CurrentPageId
                    };
                }
                else
                {
                    var httpContext = context.HttpContext;

                    // So that other actions/partials can access this.
                    httpContext.Items["SelectedNode"] = selectedNode;

                    // Add custom meta head part (mainly for client scripts)
                    var nodeType = (selectedNode.Value.EntityName ?? WebViewPageHelper.CurrentPageType).ToLowerInvariant();
                    object nodeId = selectedNode.Id;
                    if (WebViewPageHelper.IsHomePage)
                    {
                        nodeId = 0;
                    }
                    else if (nodeType == "system")
                    {
                        nodeId = WebViewPageHelper.CurrentPageId;
                    }

                    nodeData = new
                    {
                        type = nodeType,
                        id = nodeId,
                        menuItemId = selectedNode.Value.MenuItemId,
                        entityId = selectedNode.Value.EntityId,
                        parentId = selectedNode.Parent?.IsRoot == true ? 0 : selectedNode.Parent?.Id
                    };
                }

                // Add node data to head meta property as JSON.
                _widgetProvider.RegisterWidget(
                    "head",
                    new HtmlWidgetInvoker(new HtmlString("<meta property='sm:pagedata' content='{0}' />".FormatInvariant(JsonConvert.SerializeObject(nodeData)))));

                await next();
            }

            public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
            {
                if (!context.Result?.IsHtmlViewResult() == false)
                {
                    return;
                }

                await ProcessUserMenusAsync();

                await next();
            }

            private async Task<TreeNode<MenuItem>> ResolveCurrentNodeAsync(ActionExecutingContext filterContext)
            {
                // Ensure page helper is initialized
                //_pageHelper.Initialize(filterContext);

                if (WebViewPageHelper.IsHomePage)
                {
                    return await _menuService.GetRootNodeAsync("Main");
                }

                foreach (var menuName in await _menuStorage.GetMenuSystemNamesAsync(true))
                {
                    var selectedNode = await (await _menuService.GetMenuAsync(menuName))?.ResolveCurrentNodeAsync(filterContext);
                    if (selectedNode != null)
                    {
                        return selectedNode;
                    }
                }

                return null;
            }

            /// <summary>
            /// Registers actions to render user menus in widget zones.
            /// </summary>
            private async Task ProcessUserMenusAsync()
            {
                var menusInfo = await _menuStorage.GetUserMenuInfosAsync();

                foreach (var info in menusInfo)
                {
                    // TODO: (mh) (core) How to register action?
                    //_widgetProvider.RegisterAction(
                    //    info.WidgetZones,
                    //    "Menu",
                    //    "Menu",
                    //    new { area = "", name = info.SystemName, template = info.Template },
                    //    info.DisplayOrder);
                }
            }
        }
    }
}
