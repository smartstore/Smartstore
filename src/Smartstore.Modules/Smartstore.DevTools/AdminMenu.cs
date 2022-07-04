using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.DevTools
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            var menuItem = new MenuItem().ToBuilder()
                .Text("Developer Tools")
                .ResKey("Plugins.FriendlyName.SmartStore.DevTools")
                .Icon("terminal", "bi")
                .PermissionNames(DevToolsPermissions.Read)
                .Action("Configure", "DevTools", new { area = "Admin" })
                .AsItem();

            modulesNode.Append(menuItem);

            #region Sample

            // Uncomment to add to admin menu (see module sub-menu)
            //var backendExtensionItem = new MenuItem().ToBuilder()
            //	.Text("Backend extension")
            //	.Icon("chart-area")
            //	.Action("BackendExtension", "DevTools", new { area = "Admin" })
            //	.AsItem();
            //modulesNode.Append(backendExtensionItem);

            // Uncomment to add a sub-menu (see plugin sub-menu)
            //var subMenu = new MenuItem().ToBuilder()
            //	.Text("Sub Menu")
            //	.Action("BackendExtension", "DevTools", new { area = "Admin" })
            //	.AsItem();
            //var subMenuNode = modulesNode.Append(subMenu);

            //var subMenuItem = new MenuItem().ToBuilder()
            //	.Text("Sub Menu Item 1")
            //	.Action("BackendExtension", "DevTools", new { area = "Admin" })
            //	.AsItem();
            //subMenuNode.Append(subMenuItem);

            #endregion
        }
    }
}
