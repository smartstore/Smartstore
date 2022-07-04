using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Clickatell
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            var menuItem = new MenuItem().ToBuilder()
                .Text("Clickatell SMS Provider")
                .ResKey("Plugins.FriendlyName.SmartStore.Clickatell")
                .Icon("send", "bi")
                .Action("Configure", "Clickatell", new { area = "Admin" })
                .AsItem();

            modulesNode.Append(menuItem);
        }
    }
}
