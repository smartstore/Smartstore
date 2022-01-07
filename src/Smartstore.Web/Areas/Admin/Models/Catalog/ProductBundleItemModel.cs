using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.BundleItems.Fields.")]
    public class ProductBundleItemModel : EntityModelBase, ILocalizedModel<ProductBundleItemLocalizedModel>
    {
        public List<ProductBundleItemLocalizedModel> Locales { get; set; } = new();
        public List<ProductBundleItemAttributeModel> Attributes { get; set; } = new();

        public int ProductId { get; set; }
        public int BundleProductId { get; set; }
        public bool IsPerItemPricing { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        public string ShortDescription { get; set; }

        [LocalizedDisplay("*Quantity")]
        public int Quantity { get; set; }

        [LocalizedDisplay("*Discount")]
        public decimal? Discount { get; set; }

        [LocalizedDisplay("*DiscountPercentage")]
        public bool DiscountPercentage { get; set; }

        [LocalizedDisplay("*FilterAttributes")]
        public bool FilterAttributes { get; set; }

        [LocalizedDisplay("*HideThumbnail")]
        public bool HideThumbnail { get; set; }

        [LocalizedDisplay("*Visible")]
        public bool Visible { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("Common.UpdatedOn")]
        public DateTime UpdatedOn { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.Products.BundleItems.Fields.")]
    public class ProductBundleItemLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        public string ShortDescription { get; set; }
    }

    public class ProductBundleItemAttributeModel : EntityModelBase
    {
        public static string AttributeControlPrefix => "attribute_";
        public static string PreSelectControlPrefix => "preselect_";

        public string AttributeControlId => AttributeControlPrefix + Id.ToString();
        public string PreSelectControlId => PreSelectControlPrefix + Id.ToString();

        public string Name { get; set; }

        public List<SelectListItem> Values { get; set; } = new();
        public List<SelectListItem> PreSelect { get; set; } = new();
    }
}
