using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Categories.Fields.")]
    public class CategoryModel : TabbableModel, ILocalizedModel<CategoryLocalizedModel>
    {
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
        public int BadgeStyle { get; set; }

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
        [AdditionalMetadata("album", "catalog"), AdditionalMetadata("transientUpload", true)]
        [LocalizedDisplay("*Picture")]
        public int? PictureId { get; set; }

        [LocalizedDisplay("*PageSize")]
        public int? PageSize { get; set; }

        [LocalizedDisplay("*AllowCustomersToSelectPageSize")]
        public bool? AllowCustomersToSelectPageSize { get; set; }

        [LocalizedDisplay("*PageSizeOptions")]
        public string PageSizeOptions { get; set; }

        [LocalizedDisplay("*ShowOnHomePage")]
        public bool ShowOnHomePage { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

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

    public partial class CategoryValidator : AbstractValidator<CategoryModel>
    {
        public CategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    public class CategoryMapper :
        IMapper<Category, CategoryModel>,
        IMapper<CategoryModel, Category>
    {
        private readonly ICommonServices _services;
        private readonly IUrlHelper _urlHelper;
        private readonly ICategoryService _categoryService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IAclService _aclService;

        public CategoryMapper(
            ICommonServices services, 
            IUrlHelper urlHelper, 
            ICategoryService categoryService,
            IStoreMappingService storeMappingService,
            IAclService aclService)
        {
            _services = services;
            _urlHelper = urlHelper;
            _categoryService = categoryService;
            _storeMappingService = storeMappingService;
            _aclService = aclService;
        }

        public async Task MapAsync(Category from, CategoryModel to, dynamic parameters = null)
        {
            await _services.DbContext.LoadCollectionAsync(from, x => x.AppliedDiscounts);
            await _services.DbContext.LoadCollectionAsync(from, x => x.RuleSets);

            MiniMapper.Map(from, to);
            to.SeName = await from.GetActiveSlugAsync(0, true, false);
            to.PictureId = from.MediaFileId;
            to.UpdatedOn = _services.DateTimeHelper.ConvertToUserTime(from.UpdatedOnUtc, DateTimeKind.Utc);
            to.CreatedOn = _services.DateTimeHelper.ConvertToUserTime(from.CreatedOnUtc, DateTimeKind.Utc);

            to.Breadcrumb = await _categoryService.GetCategoryPathAsync(from, _services.WorkContext.WorkingLanguage.Id, "<span class='badge badge-secondary'>{0}</span>");
            to.EditUrl = _urlHelper.Action("Edit", "Category", new { id = from.Id, area = "Admin" });

            to.SelectedDiscountIds = from.AppliedDiscounts.Select(x => x.Id).ToArray();
            to.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(from);
            to.SelectedCustomerRoleIds = await _aclService.GetAuthorizedCustomerRoleIdsAsync(from);
            to.SelectedRuleSetIds = from.RuleSets.Select(x => x.Id).ToArray();
        }

        public Task MapAsync(CategoryModel from, Category to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.MediaFileId = from.PictureId.ZeroToNull();

            return Task.CompletedTask;
        }
    }
}
