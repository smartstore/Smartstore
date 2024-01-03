using System.Xml;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;

namespace Smartstore.Admin.Infrastructure.Menus
{
    public partial class AdminMenu : MenuBase
    {
        public override string Name => "Admin";

        public override bool ApplyPermissions => true;

        protected override string GetCacheKey()
        {
            var cacheKey = "{0}-{1}".FormatInvariant(
                Services.WorkContext.WorkingLanguage.Id,
                Services.WorkContext.CurrentCustomer.GetRolesIdent());

            return cacheKey;
        }

        protected override Task<TreeNode<MenuItem>> BuildAsync(CacheEntryOptions cacheEntryOptions)
        {
            var contentRoot = Services.ApplicationContext.ContentRoot;
            var file = contentRoot.GetFile("/Areas/Admin/sitemap.xml");
            var xmlSitemap = new XmlDocument();
            xmlSitemap.Load(file.PhysicalPath);

            var rootNode = ConvertXmlNodeToMenuItemNode(xmlSitemap.DocumentElement.FirstChild as XmlElement);
            return Task.FromResult(rootNode);
        }

        protected override Task OnMenuBuilt(TreeNode<MenuItem> root)
        {
            // Rearrange order of "Plugins" child nodes (ensure that "Manage Plugins" comes last).
            var modulesNode = root.SelectNodeById("modules");
            var sepNode = root.SelectNodeById("modules-sep-1");
            var manageNode = root.SelectNodeById("modules-manage");

            if (sepNode != null)
            {
                modulesNode.Append(sepNode);
            }

            if (manageNode != null)
            {
                modulesNode.Append(manageNode);
            }

            return Task.CompletedTask;
        }

        protected virtual TreeNode<MenuItem> ConvertXmlNodeToMenuItemNode(XmlElement node)
        {
            var id = node.GetAttribute("id");
            var routeName = node.GetAttribute("routeName");
            var controller = node.GetAttribute("controller");
            var action = node.GetAttribute("action");
            var url = node.GetAttribute("url");
            var icon = node.GetAttribute("icon").NullEmpty();

            var item = new MenuItem 
            {
                Id = id,
                Text = node.GetAttribute("title").NullEmpty(),
                PermissionNames = node.GetAttribute("permissionNames").NullEmpty(),
                ResKey = node.GetAttribute("resKey").NullEmpty(),
                IconClass = node.GetAttribute("iconClass").NullEmpty(),
                ImageUrl = node.GetAttribute("imageUrl").NullEmpty()
            };
            
            var root = new TreeNode<MenuItem>(item, id);

            if (icon.HasValue())
            {
                if (icon.StartsWith("bi:"))
                {
                    item.IconLibrary = "bi";
                    item.Icon = icon[3..];
                }
                else
                {
                    item.Icon = icon;
                }
            }

            if (node.HasAttribute("isGroupHeader"))
            {
                item.IsGroupHeader = node.GetAttribute("isGroupHeader").ToBool();
            }

            if (routeName.HasValue())
            {
                item.RouteName = routeName;
            }
            else if (action.HasValue() && controller.HasValue())
            {
                item.ActionName = action;
                item.ControllerName = controller;

            }
            else if (url.HasValue())
            {
                item.Url = url;
            }

            if (node.HasAttribute("area"))
            {
                item.RouteValues["area"] = node.GetAttribute("area");
            }

            // Iterate children recursively.
            foreach (var childNode in node.ChildNodes.OfType<XmlElement>())
            {
                var childTreeNode = ConvertXmlNodeToMenuItemNode(childNode);
                root.Append(childTreeNode);
            }

            return root;
        }
    }
}
