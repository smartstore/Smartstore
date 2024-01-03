using Smartstore.Collections;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Content.Menus
{
    // INFO: The provider's SystemName is also the edit template name > Views/Shared/EditorTemplates/MenuItem.{SystemName}.cshtml.
    // Model is: string
    [MenuItemProvider("entity")]
    public class EntityMenuItemProvider : MenuItemProviderBase
    {
        private readonly ILinkResolver _linkResolver;

        public EntityMenuItemProvider(ILinkResolver linkResolver)
        {
            _linkResolver = linkResolver;
        }

        protected override async Task ApplyLinkAsync(MenuItemProviderRequest request, TreeNode<MenuItem> node)
        {
            // Always resolve against current store, current customer and working language.
            var result = await _linkResolver.ResolveAsync(request.Entity.Model);
            var item = node.Value;

            item.Url = result.Link;
            item.ImageId = result.PictureId;

            if (item.Text.IsEmpty())
            {
                item.Text = result.Label;
            }

            if (result.EntityId.HasValue && !request.IsEditMode)
            {
                item.EntityId = result.EntityId.Value;
                item.EntityName = result.EntityName;
            }

            if (request.IsEditMode)
            {
                var info = _linkResolver.GetBuilderMetadata().FirstOrDefault(x => x.Schema == result.Expression.Schema);

                if (info != null)
                {
                    item.Summary = T(info.ResKey);
                    item.Icon = info.Icon;
                }

                if (info == null || item.Url.IsEmpty())
                {
                    item.Text = null;
                    item.ResKey = "Admin.ContentManagement.Menus.SpecifyLinkTarget";
                }
            }
            else
            {
                // For edit mode, only apply MenuItemRecord.Published.
                item.Visible = result.Status == LinkStatus.Ok;
            }
        }
    }
}
