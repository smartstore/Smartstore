using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.News
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            // Insert menu items for list views.
            var newsMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.News")
                .Icon("far fa-newspaper")
                .PermissionNames(NewsPermissions.Self)
                .AsItem();

            var newsItemsMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.News.NewsItems")
                .Action("List", "News", new { area = "Admin" })
                .AsItem();

            var newsCommentsMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.News.Comments")
                .Action("Comments", "News", new { area = "Admin" })
                .AsItem();

            // TODO: (mh) (core) Insert after blog.
            var newsNode = new TreeNode<MenuItem>(newsMenuItem);
            var parent = modulesNode.Root.SelectNodeById("cms");
            var menus = parent.SelectNodeById("menus");
            newsNode.InsertAfter(menus);

            var newsItemsNode = new TreeNode<MenuItem>(newsItemsMenuItem);
            var newsCommentsNode = new TreeNode<MenuItem>(newsCommentsMenuItem);
            newsNode.Append(newsItemsNode);
            newsNode.Append(newsCommentsNode);
        }
    }
}
