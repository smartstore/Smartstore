using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Menus
{
    [LocalizedDisplay("Admin.ContentManagement.Menus.")]
    public class MenuEntityModel : TabbableModel, ILocalizedModel<MenuEntityLocalizedModel>
    {
        [LocalizedDisplay("*SystemName")]
        public string SystemName { get; set; }

        public bool IsSystemMenu { get; set; }

        [LocalizedDisplay("*Template")]
        public string Template { get; set; }
        public bool IsCustomTemplate { get; set; }
        public List<SelectListItem> AllTemplates { get; set; } = new();

        [LocalizedDisplay("*WidgetZone")]
        [UIHint("WidgetZone")]
        public string[] WidgetZone { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("*DisplayOrder")]
        public int DisplayOrder { get; set; }

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

        public List<MenuEntityLocalizedModel> Locales { get; set; } = new();
        public List<SelectListItem> AllProviders { get; set; } = new();
        public TreeNode<MenuItem> ItemTree { get; set; }

        public string EditUrl { get; set; }
    }

    [LocalizedDisplay("Admin.ContentManagement.Menus.")]
    public class MenuEntityLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }
    }

    public partial class MenuEntityValidator : AbstractValidator<MenuEntityModel>
    {
        public MenuEntityValidator(Localizer T)
        {
            RuleFor(x => x.SystemName).NotEmpty();
        }
    }
}
