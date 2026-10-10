using Smartstore.Collections;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Menus;

[MenuItemProvider("route")]
public class RouteMenuItemProvider : MenuItemProviderBase
{
    protected override Task ApplyLinkAsync(MenuItemProviderRequest request, TreeNode<MenuItem> node)
    {
        CommonHelper.TryAction(() => node.ApplyRouteData(request.Entity.Model));

        if (request.IsEditMode)
        {
            var item = node.Value;

            item.Summary = T("Providers.MenuItems.FriendlyName.Route");
            item.Icon = "fa:route!@s";

            if (!item.HasRoute)
            {
                item.Text = null;
                item.ResKey = "Admin.ContentManagement.Menus.SpecifyLinkTarget";
            }
        }

        return Task.CompletedTask;
    }
}