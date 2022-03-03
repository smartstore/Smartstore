using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Forums
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

            var forumNode = new TreeNode<MenuItem>(new MenuItem().ToBuilder()
                .Text("Manage forums")
                .ResKey("Admin.ContentManagement.Forums")
                .Icon("chat-square-text", "bi")
                .PermissionNames(ForumPermissions.Self)
                .Action("List", "Forum", new { area = "Admin" })
                .AsItem());

            var refNode = cms.SelectNodeById("message-templates") ?? cms.SelectNodeById("polls") ?? cms.SelectNodeById("widgets");

            if (refNode != null)
            {
                forumNode.InsertBefore(refNode);
            }
            else
            {
                // Fallback.
                cms.Append(forumNode);
            }
        }
    }
}
