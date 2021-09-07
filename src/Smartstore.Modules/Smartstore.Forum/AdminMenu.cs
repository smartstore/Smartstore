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
            var messageTemplates = cms.SelectNodeById("message-templates");

            var forumNode = new TreeNode<MenuItem>(new MenuItem().ToBuilder()
                .Text("Manage forums")
                .ResKey("Admin.ContentManagement.Forums")
                .Icon("fa fa-users")
                .PermissionNames(ForumPermissions.Self)
                .Action("Configure", "Forum", new { area = "Admin" })
                .AsItem());

            forumNode.InsertBefore(messageTemplates);
        }
    }
}
