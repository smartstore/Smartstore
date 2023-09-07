using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class ProductOverviewModel : TabbableModel
    {
        [LocalizedDisplay("*ID")]
        public override int Id { get; set; }

        public string EditUrl { get; set; }

        [LocalizedDisplay("*PictureThumbnailUrl")]
        public string PictureThumbnailUrl { get; set; }
        public bool NoThumb { get; set; }

        [LocalizedDisplay("*ProductType")]
        public int ProductTypeId { get; set; }

        [LocalizedDisplay("*ProductType")]
        public string ProductTypeName { get; set; }
        public string ProductTypeLabelHint { get; set; }

        [LocalizedDisplay("*ProductUrl")]
        public string ProductUrl { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*ShowOnHomePage")]
        public bool ShowOnHomePage { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int HomePageDisplayOrder { get; set; }

        [LocalizedDisplay("*Sku")]
        public string Sku { get; set; }

        [LocalizedDisplay("*ManufacturerPartNumber")]
        public string ManufacturerPartNumber { get; set; }

        [LocalizedDisplay("*GTIN")]
        public string Gtin { get; set; }

        [LocalizedDisplay("*CustomsTariffNumber")]
        public string CustomsTariffNumber { get; set; }

        [LocalizedDisplay("*IsTaxExempt")]
        public bool IsTaxExempt { get; set; }

        [UIHint("DeliveryTimes")]
        [LocalizedDisplay("*DeliveryTime")]
        public int? DeliveryTimeId { get; set; }
        public string DeliveryTime { get; set; }

        [LocalizedDisplay("*StockQuantity")]
        public int StockQuantity { get; set; }

        [LocalizedDisplay("*MinStockQuantity")]
        public int MinStockQuantity { get; set; }

        [LocalizedDisplay("*Price")]
        public decimal Price { get; set; }

        [LocalizedDisplay("*ComparePrice")]
        public decimal? ComparePrice { get; set; }

        [LocalizedDisplay("*SpecialPrice")]
        public decimal? SpecialPrice { get; set; }

        [LocalizedDisplay("*SpecialPriceStartDateTimeUtc")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? SpecialPriceStartDateTimeUtc { get; set; }

        [LocalizedDisplay("*SpecialPriceEndDateTimeUtc")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? SpecialPriceEndDateTimeUtc { get; set; }

        [LocalizedDisplay("*Weight")]
        public decimal? Weight { get; set; }

        [LocalizedDisplay("*Length")]
        public decimal? Length { get; set; }

        [LocalizedDisplay("*Width")]
        public decimal? Width { get; set; }

        [LocalizedDisplay("*Height")]
        public decimal? Height { get; set; }

        [LocalizedDisplay("*AvailableStartDateTime")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? AvailableStartDateTimeUtc { get; set; }

        [LocalizedDisplay("*AvailableEndDateTime")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? AvailableEndDateTimeUtc { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? CreatedOn { get; set; }

        [LocalizedDisplay("Common.UpdatedOn")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? UpdatedOn { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public bool SubjectToAcl { get; set; }

        [LocalizedDisplay("Common.NumberOfOrders")]
        public int NumberOfOrders { get; set; }
        public bool HasOrders
            => NumberOfOrders > 0;

        public CopyProductModel CopyProductModel { get; set; } = new();
    }

    public partial class ProductOverviewModelValidator : SmartValidator<ProductOverviewModel>
    {
        public ProductOverviewModelValidator(SmartDbContext db)
        {
            ApplyEntityRules<Product>(db);
        }
    }
}