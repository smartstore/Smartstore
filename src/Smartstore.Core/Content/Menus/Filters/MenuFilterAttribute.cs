using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Content.Menus
{
    public class MenuFilterAttribute : TypeFilterAttribute
    {
        public MenuFilterAttribute()
            : base(typeof(MenuFilter))
        {
        }

        class MenuFilter : IAsyncActionFilter, IAsyncResultFilter
        {
            private readonly IMenuStorage _menuStorage;
            private readonly IMenuService _menuService;
            private readonly IWidgetProvider _widgetProvider;
            private readonly IPageAssetBuilder _assetBuilder;
            private readonly IDisplayHelper _displayHelper;

            public MenuFilter(
                IMenuStorage menuStorage,
                IMenuService menuService,
                IWidgetProvider widgetProvider,
                IPageAssetBuilder assetBuilder,
                IDisplayHelper displayHelper)
            {
                _menuStorage = menuStorage;
                _menuService = menuService;
                _widgetProvider = widgetProvider;
                _assetBuilder = assetBuilder;
                _displayHelper = displayHelper;
            }

            /// <summary>
            /// Find the selected node in any registered menu
            /// </summary>
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var executedContext = await next();

                if (context.HttpContext.Request.IsAjax())
                    return;

                if (!executedContext.Result.IsHtmlViewResult())
                    return;

                if (context.HttpContext.Request.IsAdminArea())
                    return;

                var selectedNode = await ResolveCurrentNodeAsync(executedContext);

                object nodeData;

                if (selectedNode == null)
                {
                    nodeData = new
                    {
                        type = _displayHelper.CurrentPageType(),
                        id = _displayHelper.CurrentPageId()
                    };
                }
                else
                {
                    var httpContext = context.HttpContext;

                    // So that other actions/partials can access this.
                    httpContext.Items["SelectedNode"] = selectedNode;

                    // Add custom meta head part (mainly for client scripts)
                    var nodeType = (selectedNode.Value.EntityName ?? _displayHelper.CurrentPageType()).ToLowerInvariant();
                    object nodeId = selectedNode.Id;
                    if (_displayHelper.IsHomePage())
                    {
                        nodeId = 0;
                    }
                    else if (nodeType == "system")
                    {
                        nodeId = _displayHelper.CurrentPageId();
                    }

                    nodeData = new
                    {
                        type = nodeType,
                        id = nodeId?.ToString(),
                        menuItemId = selectedNode.Value.MenuItemId,
                        entityId = selectedNode.Value.EntityId,
                        parentId = selectedNode.Parent?.IsRoot == true ? 0 : selectedNode.Parent?.Id
                    };
                }

                // Add node data to head meta property as JSON.
                _assetBuilder.AddHtmlContent("head", new HtmlString($"<meta property='sm:pagedata' content='{JsonConvert.SerializeObject(nodeData)}' />"));
            }

            public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
            {
                if (context.Result.IsHtmlViewResult())
                {
                    await ProcessUserMenusAsync();
                }

                await next();
            }

            private async Task<TreeNode<MenuItem>> ResolveCurrentNodeAsync(ActionExecutedContext filterContext)
            {
                if (_displayHelper.IsHomePage())
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
                    var widget = new ComponentWidget("Menu", new
                    {
                        name = info.SystemName,
                        template = info.Template
                    })
                    {
                        Order = info.DisplayOrder
                    };

                    _widgetProvider.RegisterWidget(info.WidgetZones, widget);
                }
            }
        }
    }
}
