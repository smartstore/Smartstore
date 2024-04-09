using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Seo;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Manufacturers.Fields.")]
    public class ManufacturerModel : TabbableModel, ILocalizedModel<ManufacturerLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*Description")]
        public string Description { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*BottomDescription")]
        public string BottomDescription { get; set; }

        [LocalizedDisplay("*ManufacturerTemplate")]
        public int ManufacturerTemplateId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [UIHint("Media")]
        [AdditionalMetadata("album", "catalog"), AdditionalMetadata("transientUpload", true), AdditionalMetadata("entityType", "Manufacturer")]
        [LocalizedDisplay("*Picture")]
        public int? PictureId { get; set; }

        [LocalizedDisplay("*PageSize")]
        public int? PageSize { get; set; }

        [LocalizedDisplay("*AllowCustomersToSelectPageSize")]
        public bool? AllowCustomersToSelectPageSize { get; set; }

        [LocalizedDisplay("*PageSizeOptions")]
        public string PageSizeOptions { get; set; }

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

        public List<ManufacturerLocalizedModel> Locales { get; set; } = new();

        public string EditUrl { get; set; }
        public string ManufacturerUrl { get; set; }

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
        [AdditionalMetadata("discountType", DiscountType.AssignedToManufacturers)]
        [LocalizedDisplay("Admin.Promotions.Discounts.AppliedDiscounts")]
        public int[] SelectedDiscountIds { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.Manufacturers.Fields.")]
    public class ManufacturerLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*Description")]
        public string Description { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*BottomDescription")]
        public string BottomDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }
    }

    public partial class ManufacturerValidator : SmartValidator<ManufacturerModel>
    {
        public ManufacturerValidator(SmartDbContext db)
        {
            ApplyEntityRules<Manufacturer>(db);
        }
    }

    [Mapper(Lifetime = ServiceLifetime.Singleton)]
    public class ManufacturerMapper :
        IMapper<Manufacturer, ManufacturerModel>,
        IMapper<ManufacturerModel, Manufacturer>
    {
        public async Task MapAsync(Manufacturer from, ManufacturerModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.SeName = await from.GetActiveSlugAsync(0, true, false);
            to.PictureId = from.MediaFileId;
        }

        public Task MapAsync(ManufacturerModel from, Manufacturer to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.MediaFileId = from.PictureId.ZeroToNull();

            return Task.CompletedTask;
        }
    }
}
