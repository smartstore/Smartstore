using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Forum
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            var menuItem = new MenuItem().ToBuilder()
                .Text("Manage forums")
                .ResKey("Admin.ContentManagement.Forums")
                .Icon("fa fa-users")
                .PermissionNames(ForumPermissions.Cms.Forum.Read)
                .Action("Configure", "Forum", new { area = "Admin" })
                .AsItem();

            modulesNode.Prepend(menuItem);
        }
    }
}
