using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Rules;
using Smartstore.Core.Seo;
using Smartstore.IconKit;
using Smartstore.Web.Models.Media;

namespace Smartstore.Admin.Models.Catalog;

[LocalizedDisplay("Admin.Catalog.Categories.Fields.")]
public class CategoryModel : TabbableModel, ILocalizedModel<CategoryLocalizedModel>
{
    public Type GetEntityType() => typeof(Category);

    [LocalizedDisplay("*Name")]
    public string Name { get; set; }

    [LocalizedDisplay("*FullName")]
    public string FullName { get; set; }

    [UIHint("Html")]
    [LocalizedDisplay("*Description")]
    public string Description { get; set; }

    [UIHint("Html")]
    [LocalizedDisplay("*BottomDescription")]
    public string BottomDescription { get; set; }

    [LocalizedDisplay("*ExternalLink")]
    [UIHint("Link")]
    public string ExternalLink { get; set; }

    [LocalizedDisplay("*BadgeText")]
    public string BadgeText { get; set; }

    [LocalizedDisplay("*BadgeStyle")]
    [UIHint("BadgeStyles")]
    [AdditionalMetadata("badge-text-resource", "Common.Example")]
    public string BadgeStyle { get; set; }

    [LocalizedDisplay("*Icon")]
    [UIHint("IconBrowser")]
    public IconBrowserModel IconModel { get; set; } = new()
    {
        Area = "public",
        VisibleOptions = IconOptionFields.Animation | IconOptionFields.Color | IconOptionFields.Flip | IconOptionFields.Rotate
    };

    [LocalizedDisplay("*Alias")]
    public string Alias { get; set; }

    [LocalizedDisplay("*CategoryTemplate")]
    public int CategoryTemplateId { get; set; }

    [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
    public string MetaKeywords { get; set; }

    [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
    public string MetaDescription { get; set; }

    [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
    public string MetaTitle { get; set; }

    [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
    public string SeName { get; set; }

    [LocalizedDisplay("*Parent")]
    public int? ParentCategoryId { get; set; }

    [UIHint("Media")]
    [AdditionalMetadata("album", "catalog"), AdditionalMetadata("transientUpload", true), AdditionalMetadata("entityType", "Category")]
    [LocalizedDisplay("*Picture")]
    public int? PictureId { get; set; }

    [LocalizedDisplay("*PageSize")]
    public int? PageSize { get; set; }

    [LocalizedDisplay("*AllowCustomersToSelectPageSize")]
    public bool? AllowCustomersToSelectPageSize { get; set; }

    [UIHint("SelectList")]
    [AdditionalMetadata("multiple", true)]
    [AdditionalMetadata("data_tags", true)]
    [LocalizedDisplay("*PageSizeOptions")]
    public int[] PageSizeOptions { get; set; } = [];

    [LocalizedDisplay("*Published")]
    public bool Published { get; set; }

    [LocalizedDisplay("*IgnoreInMenus")]
    public bool IgnoreInMenus { get; set; }

    [LocalizedDisplay("*ShowOnHomePage")]
    public bool ShowOnHomePage { get; set; }

    [LocalizedDisplay("*WithdrawalPeriodDays")]
    public int? WithdrawalPeriodDays { get; set; }

    [LocalizedDisplay("*AllowWithdrawalBeforeShipping")]
    public bool? AllowWithdrawalBeforeShipping { get; set; }

    [LocalizedDisplay("*Deleted")]
    public bool Deleted { get; set; }

    [LocalizedDisplay("Common.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [LocalizedDisplay("Common.CreatedOn")]
    public DateTime? CreatedOn { get; set; }

    [LocalizedDisplay("Common.UpdatedOn")]
    public DateTime? UpdatedOn { get; set; }

    public List<CategoryLocalizedModel> Locales { get; set; } = new();

    [LocalizedDisplay("Admin.Catalog.Products.Categories.Fields.Category")]
    public string Breadcrumb { get; set; }

    public string EditUrl { get; set; }
    public string CategoryUrl { get; set; }

    // ACL.
    [UIHint("CustomerRoles")]
    [AdditionalMetadata("multiple", true)]
    [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
    public int[] SelectedCustomerRoleIds { get; set; }

    [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
    public bool SubjectToAcl { get; set; }

    // Store mapping.
    [UIHint("Stores")]
    [AdditionalMetadata("multiple", true)]
    [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
    public int[] SelectedStoreIds { get; set; }

    [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
    public bool LimitedToStores { get; set; }

    [UIHint("Discounts")]
    [AdditionalMetadata("multiple", true)]
    [AdditionalMetadata("discountType", DiscountType.AssignedToCategories)]
    [LocalizedDisplay("Admin.Promotions.Discounts.AppliedDiscounts")]
    public int[] SelectedDiscountIds { get; set; }

    [LocalizedDisplay("Admin.Configuration.Settings.Catalog.DefaultViewMode")]
    public string DefaultViewMode { get; set; }

    [UIHint("RuleSets")]
    [AdditionalMetadata("multiple", true)]
    [AdditionalMetadata("scope", RuleScope.Product)]
    [LocalizedDisplay("Admin.Catalog.Categories.AutomatedAssignmentRules")]
    public int[] SelectedRuleSetIds { get; set; }
}

[LocalizedDisplay("Admin.Catalog.Categories.Fields.")]
public class CategoryLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [LocalizedDisplay("*Name")]
    public string Name { get; set; }

    [LocalizedDisplay("*FullName")]
    public string FullName { get; set; }

    [UIHint("Html")]
    [LocalizedDisplay("*Description")]
    public string Description { get; set; }

    [UIHint("Html")]
    [LocalizedDisplay("*BottomDescription")]
    public string BottomDescription { get; set; }

    [LocalizedDisplay("*BadgeText")]
    public string BadgeText { get; set; }

    [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
    public string MetaKeywords { get; set; }

    [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
    public string MetaDescription { get; set; }

    [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
    public string MetaTitle { get; set; }

    [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
    public string SeName { get; set; }
}

public partial class CategoryModelValidator : SmartValidator<CategoryModel>
{
    public CategoryModelValidator(SmartDbContext db)
    {
        ApplyEntityRules<Category>(db);

        RuleFor(x => x.IconModel.Icon.ToString())
            .MaximumLength(512)
            .OverridePropertyName("IconModel.Icon");

        RuleFor(x => x.WithdrawalPeriodDays)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WithdrawalPeriodDays != null);

        RuleFor(x => string.Join(',', x.PageSizeOptions ?? Array.Empty<int>()))
            .MaximumLength(200)
            .OverridePropertyName(nameof(CategoryModel.PageSizeOptions));

        RuleForEach(x => x.PageSizeOptions)
            // Associate item errors with the PageSizeOptions field.
            .OverrideIndexer((_, _, _, _) => string.Empty)
            .GreaterThan(0);
    }
}

[Mapper(Lifetime = ServiceLifetime.Singleton)]
public class CategoryMapper :
    IMapper<Category, CategoryModel>,
    IMapper<CategoryModel, Category>
{
    public async Task MapAsync(Category from, CategoryModel to, dynamic parameters = null)
    {
        MiniMapper.Map(from, to);
        to.PageSizeOptions ??= [];
        to.ParentCategoryId = from.ParentId;
        to.SeName = await from.GetActiveSlugAsync(0, true, false);
        to.PictureId = from.MediaFileId;
        to.IconModel.Icon = IconExpression.TryParse(from.Icon, out var expression) ? expression : null;
    }

    public Task MapAsync(CategoryModel from, Category to, dynamic parameters = null)
    {
        MiniMapper.Map(from, to);
        to.ParentId = from.ParentCategoryId;
        to.MediaFileId = from.PictureId.ZeroToNull();
        to.Icon = from.IconModel.Icon?.ToString().NullEmpty();

        return Task.CompletedTask;
    }
}