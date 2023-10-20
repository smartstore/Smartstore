using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Cart
{
    public abstract class CartModelBase : ModelBase
    {
        public abstract IEnumerable<CartEntityModelBase> Items { get; }

        public bool ShowSku { get; set; }
        public bool ShowProductImages { get; set; }
        public bool ShowProductBundleImages { get; set; }
        public bool IsEditable { get; set; }
        public int BundleThumbSize { get; set; }
        public bool DisplayShortDesc { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public abstract class CartEntityModelBase : EntityModelBase, IQuantityInput
    {
        public string Sku { get; set; }
        public ImageModel Image { get; set; } = new();

        public int ProductId { get; set; }
        public LocalizedValue<string> ProductName { get; set; }
        public string ProductSeName { get; set; }
        public string ProductUrl { get; set; }
        public ProductType ProductType { get; set; }
        public bool VisibleIndividually { get; set; }

        public CartItemPriceModel Price { get; set; } = new();

        public int EnteredQuantity { get; set; }
        public LocalizedValue<string> QuantityUnitName { get; set; }
        public LocalizedValue<string> QuantityUnitNamePlural { get; set; }
        public List<SelectListItem> AllowedQuantities { get; set; } = new();
        public int MinOrderAmount { get; set; }
        public int MaxOrderAmount { get; set; }
        public int QuantityStep { get; set; }
        public int? MaxInStock { get; set; }
        public QuantityControlType QuantityControlType { get; set; }

        public string AttributeInfo { get; set; }
        public string RecurringInfo { get; set; }
        public List<string> Warnings { get; set; } = new();
        public LocalizedValue<string> ShortDesc { get; set; }

        public BundleItemModel BundleItem { get; set; }
        public bool IsBundleItem
        {
            get => BundleItem != null;
        }

        public abstract IEnumerable<CartEntityModelBase> ChildItems { get; }
        public DateTime CreatedOnUtc { get; set; }
    }

    public partial class BundleItemModel : EntityModelBase
    {
        public bool PerItemPricing { get; set; }
        public bool PerItemShoppingCart { get; set; }
        public string Title { get; set; }
        public int DisplayOrder { get; set; }
        public bool HideThumbnail { get; set; }
    }

    public partial class CartItemPriceModel : ModelBase
    {
        public int Quantity { get; set; } = 1;
        public bool IsBundlePart { get; set; }

        public Money UnitPrice { get; set; }
        public Money SubTotal { get; set; }
        public Money? ShippingSurcharge { get; set; }

        public Money TotalDiscount { get; set; }

        /// <summary>
        ///  Single unit saving
        /// </summary>
        public PriceSaving Saving { get; set; }
        public DateTime? ValidUntilUtc { get; set; }
        public LocalizedString CountdownText { get; set; }

        /// <summary>
        ///  Single unit regular price
        /// </summary>
        public ComparePriceModel RegularPrice { get; set; }

        /// <summary>
        ///  Single unit retail price
        /// </summary>
        public ComparePriceModel RetailPrice { get; set; }

        /// <summary>
        ///  Single unit base price info
        /// </summary>
        public string BasePriceInfo { get; set; }

        public bool ShowRetailPriceSaving { get; set; }
        public List<ProductBadgeModel> Badges { get; } = new();
    }
}