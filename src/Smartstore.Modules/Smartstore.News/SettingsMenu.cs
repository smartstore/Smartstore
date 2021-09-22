using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.News
{
    public class SettingsMenu : IMenuProvider
    {
        void IMenuProvider.BuildMenu(TreeNode<MenuItem> rootNode)
        {
            var newsMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.Configuration.Settings.News")
                .Id("news")
                .Icon("far fa-fw fa-edit")
                .PermissionNames(NewsPermissions.Read)
                .Action("Settings", "News", new { area = "Admin" })
                .AsItem();

            var newsNode = new TreeNode<MenuItem>(newsMenuItem);
            var media = rootNode.SelectNodeById("media");
            newsNode.InsertAfter(media);
        }

        public string MenuName => "settings";

        public virtual int Ordinal => 0;
    }
}
