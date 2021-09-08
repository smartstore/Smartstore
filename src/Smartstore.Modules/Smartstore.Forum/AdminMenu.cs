using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Forum
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            var cms = modulesNode.Root.SelectNodeById("cms");
            if (cms == null)
            {
                // No CMS, no forum.
                return;
            }

            var insertBeforeNode = cms.SelectNodeById("message-templates") ?? cms.SelectNodeById("polls") ?? cms.SelectNodeById("widgets");

            var forumNode = new TreeNode<MenuItem>(new MenuItem().ToBuilder()
                .Text("Manage forums")
                .ResKey("Admin.ContentManagement.Forums")
                .Icon("fa fa-users")
                .PermissionNames(ForumPermissions.Self)
                .Action("List", "Forum", new { area = "Admin" })
                .AsItem());

            if (insertBeforeNode != null)
            {
                forumNode.InsertBefore(insertBeforeNode);
            }
            else
            {
                // Fallback.
                cms.Append(forumNode);
            }
        }
    }
}
