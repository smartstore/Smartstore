using Smartstore.Collections;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Content.Menus
{
    [MenuItemProvider("route")]
    public class RouteMenuItemProvider : MenuItemProviderBase
    {
        protected override Task ApplyLinkAsync(MenuItemProviderRequest request, TreeNode<MenuItem> node)
        {
            try
            {
                node.ApplyRouteData(request.Entity.Model);
            }
            catch
            {
            }

            if (request.IsEditMode)
            {
                var item = node.Value;

                item.Summary = T("Providers.MenuItems.FriendlyName.Route");
                item.Icon = "fas fa-route";

                if (!item.HasRoute)
                {
                    item.Text = null;
                    item.ResKey = "Admin.ContentManagement.Menus.SpecifyLinkTarget";
                }
            }

            return Task.CompletedTask;
        }
    }
}
