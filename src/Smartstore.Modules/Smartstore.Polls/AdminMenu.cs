using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Polls
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            // Insert menu items for list views.
            var pollsMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.Polls")
                .Icon("check2-circle", "bi")
                .PermissionNames(PollPermissions.Self)
                .Action("List", "PollAdmin", new { area = "Admin" })
                .AsItem();

            var pollNode = new TreeNode<MenuItem>(pollsMenuItem);
            var parent = modulesNode.Root.SelectNodeById("cms");
            var messageTemplates = parent.SelectNodeById("message-templates");
            pollNode.InsertAfter(messageTemplates);
        }
    }
}
