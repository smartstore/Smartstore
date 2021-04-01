using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.ShoppingCart
{
    public partial class WishlistModel : ModelBase
    {        
        public Guid CustomerGuid { get; set; }
        public string CustomerFullname { get; set; }

        public bool EmailWishlistEnabled { get; set; }
        public bool ShowSku { get; set; }
        public bool ShowProductImages { get; set; }
        public bool IsEditable { get; set; }
        public bool DisplayAddToCart { get; set; }

        public List<ShoppingCartItemModel> Items { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public int ThumbSize { get; set; }
        public int BundleThumbSize { get; set; }
        public bool DisplayShortDesc { get; set; }
        public bool ShowProductBundleImages { get; set; }
        public bool ShowItemsFromWishlistToCartButton { get; set; }

        #region Nested Classes

        public partial class ShoppingCartItemModel : EntityModelBase, IQuantityInput
        {
            public string Sku { get; set; }
            public ImageModel Image { get; set; } = new();

            public int ProductId { get; set; }
            public LocalizedValue<string> ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }
            public ProductType ProductType { get; set; }
            public bool VisibleIndividually { get; set; }
            public string UnitPrice { get; set; }
            public string SubTotal { get; set; }
            public string Discount { get; set; }

            public int EnteredQuantity { get; set; }
            public LocalizedValue<string> QuantityUnitName { get; set; }
            public List<SelectListItem> AllowedQuantities { get; set; } = new();
            public int MinOrderAmount { get; set; }
            public int MaxOrderAmount { get; set; }
            public int QuantityStep { get; set; }
            public QuantityControlType QuantiyControlType { get; set; }

            public string AttributeInfo { get; set; }
            public string RecurringInfo { get; set; }
            public List<string> Warnings { get; set; } = new();
            public LocalizedValue<string> ShortDesc { get; set; }

            public bool BundlePerItemPricing { get; set; }
            public bool BundlePerItemShoppingCart { get; set; }
            public BundleItemModel BundleItem { get; set; } = new();
            public List<ShoppingCartItemModel> ChildItems { get; set; } = new();

            public bool DisableBuyButton { get; set; }
            public DateTime CreatedOnUtc { get; set; }
        }

        public partial class BundleItemModel : EntityModelBase
        {
            public string PriceWithDiscount { get; set; }
            public int DisplayOrder { get; set; }
            public bool HideThumbnail { get; set; }
        }

        #endregion
    }
}