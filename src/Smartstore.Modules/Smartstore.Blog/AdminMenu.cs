using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Blog
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            // Prepend menu item for configure view.
            var menuItem = new MenuItem().ToBuilder()
                .ResKey("Plugins.CMS.Blog.Settings")
                .Icon("fa fa-blog")
                .PermissionNames(BlogPermissions.Read)
                .Action("Configure", "Blog", new { area = "Admin" })
                .AsItem();

            modulesNode.Prepend(menuItem);

            // Insert menu items for list views.
            var blogMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.Blog")
                .Icon("fa fa-blog")
                .PermissionNames(BlogPermissions.Self)
                .AsItem();

            var blogPostsMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.Blog.BlogPosts")
                .Action("List", "Blog", new { area = "Admin" })
                .AsItem();

            var blogCommentsMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.Blog.Comments")
                .Action("Comments", "Blog", new { area = "Admin" })
                .AsItem();

            var blogNode = new TreeNode<MenuItem>(blogMenuItem);
            var parent = modulesNode.Root.SelectNodeById("cms");
            var menus = parent.SelectNodeById("menus");
            blogNode.InsertAfter(menus);

            var blogPostsNode = new TreeNode<MenuItem>(blogPostsMenuItem);
            var blogCommentsNode = new TreeNode<MenuItem>(blogCommentsMenuItem);
            blogNode.Append(blogPostsNode);
            blogNode.Append(blogCommentsNode);
        }
    }
}
