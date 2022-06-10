using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Collections;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Menus
{
    [LocalizedDisplay("Admin.ContentManagement.Menus.Item.")]
    public class MenuItemModel : TabbableModel, IIcon, ILocalizedModel<MenuItemLocalizedModel>
    {
        public int MenuId { get; set; }
        public string Model { get; set; }
        public bool ProviderAppendsMultipleItems { get; set; }

        [LocalizedDisplay("*ParentItem")]
        public int? ParentItemId { get; set; }
        public List<SelectListItem> AllItems { get; set; } = new();

        [LocalizedDisplay("*LinkTarget")]
        public string ProviderName { get; set; }

        [LocalizedDisplay("Admin.ContentManagement.Menus.Title")]
        public string Title { get; set; }
        public string TitlePlaceholder { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 1)]
        [LocalizedDisplay("*ShortDescription")]
        public string ShortDescription { get; set; }

        [LocalizedDisplay("*PermissionNames")]
        [UIHint("AccessPermissions")]
        public string[] PermissionNames { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("*DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*BeginGroup")]
        public bool BeginGroup { get; set; }

        [LocalizedDisplay("*ShowExpanded")]
        public bool ShowExpanded { get; set; }

        [LocalizedDisplay("*NoFollow")]
        public bool NoFollow { get; set; }

        [LocalizedDisplay("*NewWindow")]
        public bool NewWindow { get; set; }

        [LocalizedDisplay("*Icon")]
        public string Icon { get; set; }

        public string Style { get; set; }

        [LocalizedDisplay("*IconColor")]
        public string IconColor { get; set; }

        [LocalizedDisplay("*HtmlId")]
        public string HtmlId { get; set; }

        [LocalizedDisplay("*CssClass")]
        public string CssClass { get; set; }

        // Store mapping.
        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        // ACL.
        [UIHint("CustomerRoles")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public int[] SelectedCustomerRoleIds { get; set; }

        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public bool SubjectToAcl { get; set; }

        public List<MenuItemLocalizedModel> Locales { get; set; } = new();
    }

    public class MenuItemLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.ContentManagement.Menus.Title")]
        public string Title { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 1)]
        [LocalizedDisplay("Admin.ContentManagement.Menus.Item.ShortDescription")]
        public string ShortDescription { get; set; }
    }

    public partial class MenuItemValidator : AbstractValidator<MenuItemModel>
    {
        public MenuItemValidator(Localizer T, IUrlHelper urlHelper)
        {
            RuleFor(x => x.ProviderName).NotEmpty();

            RuleFor(x => x.Model)
                .Must(x =>
                {
                    try
                    {
                        if (x.HasValue())
                        {
                            var node = new TreeNode<MenuItem>(new MenuItem());
                            node.ApplyRouteData(x);

                            var result = node.Value.GenerateUrl(urlHelper);
                            return result.HasValue();
                        }
                    }
                    catch
                    {
                    }

                    return false;
                })
                .When(x => x.ProviderName.EqualsNoCase("route"))
                .WithMessage(T("Admin.ContentManagement.Menus.Item.InvalidRouteValues"));
        }
    }
}
