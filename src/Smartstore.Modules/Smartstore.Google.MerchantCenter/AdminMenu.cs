using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Google.MerchantCenter
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            var menuItem = new MenuItem().ToBuilder()
                .Text("Google Merchant Center")
                .ResKey("Plugins.FriendlyName.SmartStore.Google.MerchantCenter")
                .Icon("google", "bi")
                .Action("Configure", "GoogleMerchantCenter", new { area = "Admin" })
                .AsItem();

            modulesNode.Append(menuItem);
        }
    }
}
