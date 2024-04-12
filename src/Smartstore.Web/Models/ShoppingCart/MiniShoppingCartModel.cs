using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Cart
{
    public partial class MiniShoppingCartModel : ModelBase
    {
        public List<ShoppingCartItemModel> Items { get; set; } = [];
        public int TotalQuantity { get; set; }
        public Money SubTotal { get; set; }
        public bool DisplayCheckoutButton { get; set; }
        public bool DisplayShoppingCartButton { get; set; }
        public bool CurrentCustomerIsGuest { get; set; }
        public bool AnonymousCheckoutAllowed { get; set; }
        public bool ShowProductImages { get; set; }
        public int ThumbSize { get; set; }
        public bool DisplayMoveToWishlistButton { get; set; }
        public bool ShowBasePrice { get; set; }

        public partial class ShoppingCartItemModel : EntityModelBase, IQuantityInput
        {
            public bool Active { get; set; }
            public int ProductId { get; set; }

            public LocalizedValue<string> ProductName { get; set; }
            public LocalizedValue<string> ShortDesc { get; set; }

            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }

            public int EnteredQuantity { get; set; }
            public int? MaxInStock { get; set; }

            public LocalizedValue<string> QuantityUnitName { get; set; }
            public LocalizedValue<string> QuantityUnitNamePlural { get; set; }
            public List<SelectListItem> AllowedQuantities { get; set; } = [];

            public int MinOrderAmount { get; set; }
            public int MaxOrderAmount { get; set; }

            public int QuantityStep { get; set; }
            public QuantityControlType QuantityControlType { get; set; }

            public Money UnitPrice { get; set; }
            public string BasePriceInfo { get; set; }

            public string AttributeInfo { get; set; }
            public string EssentialSpecAttributesInfo { get; set; }

            public ImageModel Image { get; set; } = new();

            public List<ShoppingCartItemBundleItem> BundleItems { get; set; } = [];

            public DateTime CreatedOnUtc { get; set; }
        }

        public partial class ShoppingCartItemBundleItem : ModelBase
        {
            public ImageModel ImageModel { get; set; }
            public LocalizedValue<string> ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }
        }
    }
}
