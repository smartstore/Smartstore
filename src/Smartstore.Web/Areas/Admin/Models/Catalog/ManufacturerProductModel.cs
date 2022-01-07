using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class ManufacturerProductModel : EntityModelBase
    {
        public int ManufacturerId { get; set; }
        public string EditUrl { get; set; }

        [UIHint("ManufacturerProduct")]
        [LocalizedDisplay("Admin.Catalog.Manufacturers.Products.Fields.Product")]
        public int ProductId { get; set; }
        public string ProductName { get; set; }

        [LocalizedDisplay("*Sku")]
        public string Sku { get; set; }

        [LocalizedDisplay("*ProductType")]
        public string ProductTypeName { get; set; }
        public string ProductTypeLabelHint { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("Admin.Catalog.Manufacturers.Products.Fields.IsFeaturedProduct")]
        public bool IsFeaturedProduct { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}
