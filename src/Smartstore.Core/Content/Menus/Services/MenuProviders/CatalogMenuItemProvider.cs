using Smartstore.Collections;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Menus
{
    [MenuItemProvider("catalog", AppendsMultipleItems = true)]
    public class CatalogMenuItemProvider : MenuItemProviderBase
    {
        private readonly IStoreContext _storeContext;
        private readonly ICategoryService _categoryService;
        private readonly ILinkResolver _linkResolver;

        public CatalogMenuItemProvider(
            IStoreContext storeContext,
            ICategoryService categoryService,
            ILinkResolver linkResolver)
        {
            _storeContext = storeContext;
            _categoryService = categoryService;
            _linkResolver = linkResolver;
        }

        public override async Task<TreeNode<MenuItem>> AppendAsync(MenuItemProviderRequest request)
        {
            if (request.IsEditMode)
            {
                var item = ConvertToMenuItem(request);
                item.Summary = T("Providers.MenuItems.FriendlyName.Catalog");
                item.Icon = "fa fa-cubes";

                AppendToParent(request, item);
            }
            else
            {
                var tree = await _categoryService.GetCategoryTreeAsync(0, false, _storeContext.CurrentStore.Id);
                var randomId = CommonHelper.GenerateRandomInteger(0, 1000000);

                if (request.Entity.BeginGroup)
                {
                    AppendToParent(request, new MenuItem
                    {
                        IsGroupHeader = true,
                        Text = request.Entity.GetLocalized(x => x.ShortDescription)
                    });
                }

                // Do not append the root itself.
                foreach (var child in tree.Children)
                {
                    var tuple = await ConvertNodeAsync(request, child, randomId);
                    AppendToParent(request, tuple.Item1);
                    randomId = tuple.Item2;
                }
            }

            // Do not traverse appended items.
            return null;

            // TBD: Cache invalidation workflow changes, because the catalog tree 
            // is now contained within other menus. Invalidating the tree now means:
            // invalidate all containing menus also.
        }

        protected override Task ApplyLinkAsync(MenuItemProviderRequest request, TreeNode<MenuItem> node)
            => Task.CompletedTask;

        private async Task<Tuple<TreeNode<MenuItem>, int>> ConvertNodeAsync(
            MenuItemProviderRequest request,
            TreeNode<ICategoryNode> categoryNode,
            int randomId)
        {
            var node = categoryNode.Value;
            var name = node.Id > 0 ? node.GetLocalized(x => x.Name) : null;

            var menuItem = new MenuItem
            {
                Id = randomId++.ToString(),
                EntityId = node.Id,
                EntityName = nameof(Category),
                MenuItemId = request.Entity.Id,
                MenuId = request.Entity.MenuId,
                Text = name?.Value ?? node.Name,
                Rtl = name?.CurrentLanguage?.Rtl ?? false,
                BadgeText = node.Id > 0 ? node.GetLocalized(x => x.BadgeText) : null,
                BadgeStyle = node.BadgeStyle,
                RouteName = node.Id > 0 ? "Category" : "HomePage",
                ImageId = node.MediaFileId
            };

            // Handle external link
            if (node.ExternalLink.HasValue())
            {
                var link = await _linkResolver.ResolveAsync(node.ExternalLink);
                if (link.Status == LinkStatus.Ok)
                {
                    menuItem.Url = link.Link;
                }
            }

            if (menuItem.Url.IsEmpty())
            {
                if (node.Id > 0)
                {
                    menuItem.RouteName = "Category";
                    menuItem.RouteValues.Add("SeName", await node.GetActiveSlugAsync());
                }
                else
                {
                    menuItem.RouteName = "Homepage";
                }
            }

            // Picture
            if (node.Id > 0 && node.ParentId == null && node.Published && node.MediaFileId != null)
            {
                menuItem.ImageId = node.MediaFileId;
            }

            // Apply inheritable properties.
            menuItem.Visible = request.Entity.Published;
            menuItem.PermissionNames = request.Entity.PermissionNames;

            if (request.Entity.NoFollow)
            {
                menuItem.LinkHtmlAttributes.Add("rel", "nofollow");
            }

            if (request.Entity.NewWindow)
            {
                menuItem.LinkHtmlAttributes.Add("target", "_blank");
            }

            var convertedNode = new TreeNode<MenuItem>(menuItem)
            {
                Id = categoryNode.Id
            };

            if (categoryNode.HasChildren)
            {
                foreach (var childNode in categoryNode.Children)
                {
                    var tuple = await ConvertNodeAsync(request, childNode, randomId);
                    convertedNode.Append(tuple.Item1);
                    randomId = tuple.Item2;
                }
            }

            return new Tuple<TreeNode<MenuItem>, int>(convertedNode, randomId);
        }
    }
}
