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
    public partial class MiniShoppingCartModel : ModelBase
    {
        public List<ShoppingCartItemModel> Items { get; set; } = new();
        public int TotalProducts { get; set; }
        public string SubTotal { get; set; }
        public bool DisplayCheckoutButton { get; set; }
        public bool CurrentCustomerIsGuest { get; set; }
        public bool AnonymousCheckoutAllowed { get; set; }
        public bool ShowProductImages { get; set; }
        public int ThumbSize { get; set; }
        public bool DisplayMoveToWishlistButton { get; set; }
        public bool ShowBasePrice { get; set; }

        #region Nested Classes

        public partial class ShoppingCartItemModel : EntityModelBase, IQuantityInput
        {
            public int ProductId { get; set; }

            public LocalizedValue<string> ProductName { get; set; }

            public LocalizedValue<string> ShortDesc { get; set; }

            public string ProductSeName { get; set; }

            public string ProductUrl { get; set; }

            public int EnteredQuantity { get; set; }

            public LocalizedValue<string> QuantityUnitName { get; set; }

            public List<SelectListItem> AllowedQuantities { get; set; } = new();

            public int MinOrderAmount { get; set; }

            public int MaxOrderAmount { get; set; }

            public int QuantityStep { get; set; }

            public QuantityControlType QuantiyControlType { get; set; }

            public string UnitPrice { get; set; }

            public string BasePriceInfo { get; set; }

            public string AttributeInfo { get; set; }

            public ImageModel Image { get; set; } = new();

            public List<ShoppingCartItemBundleItem> BundleItems { get; set; } = new();

            public DateTime CreatedOnUtc { get; set; }

        }

        public partial class ShoppingCartItemBundleItem : ModelBase
        {
            public string PictureUrl { get; set; }
            public LocalizedValue<string> ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }
        }

        #endregion
    }
}
