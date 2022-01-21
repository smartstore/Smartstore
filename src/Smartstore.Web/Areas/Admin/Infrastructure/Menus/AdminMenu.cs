using System.Xml;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Infrastructure.Menus
{
    public partial class AdminMenu : MenuBase
    {
        public Localizer T { get; set; }

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

        protected virtual TreeNode<MenuItem> ConvertXmlNodeToMenuItemNode(XmlElement node)
        {
            var item = new MenuItem();
            var root = new TreeNode<MenuItem>(item);

            var id = node.GetAttribute("id");
            var routeName = node.GetAttribute("routeName");
            var controller = node.GetAttribute("controller");
            var action = node.GetAttribute("action");
            var url = node.GetAttribute("url");

            root.Id = id;

            item.Id = id;
            item.Text = node.GetAttribute("title").NullEmpty();
            item.PermissionNames = node.GetAttribute("permissionNames").NullEmpty();
            item.ResKey = node.GetAttribute("resKey").NullEmpty();
            item.Icon = node.GetAttribute("iconClass").NullEmpty();
            item.ImageUrl = node.GetAttribute("imageUrl").NullEmpty();

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
