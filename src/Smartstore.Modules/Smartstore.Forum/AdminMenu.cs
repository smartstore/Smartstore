using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Forum
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            // TODO: (mg) (core) Restore menu structure exactly as it was before (CMS --> Topics | [News] | [Blogs] | FORUM).
            // Means: After Blogs or News or Topics. Module Order is important here.
            // INFO: (mg) (core) Generally speaking: dont't do extra plugin-centric stuff here. Just restore things as they were before.
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
