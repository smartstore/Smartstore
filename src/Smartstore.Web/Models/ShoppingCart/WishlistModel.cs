using System;
using System.Collections.Generic;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Models.ShoppingCart
{
    public partial class WishlistModel : CartModelBase
    {
        public override IEnumerable<CartEntityModelBase> Items { get; set; } = new List<ShoppingCartItemModel>();

        public Guid CustomerGuid { get; set; }
        public string CustomerFullname { get; set; }

        public bool EmailWishlistEnabled { get; set; }
        public bool ShowSku { get; set; }
        public bool ShowProductImages { get; set; }
        public bool IsEditable { get; set; }
        public bool DisplayAddToCart { get; set; }

        public List<string> Warnings { get; set; } = new();

        public int ThumbSize { get; set; }
        public int BundleThumbSize { get; set; }
        public bool DisplayShortDesc { get; set; }
        public bool ShowProductBundleImages { get; set; }
        public bool ShowItemsFromWishlistToCartButton { get; set; }

        #region Nested Classes

        public partial class ShoppingCartItemModel : CartEntityModelBase, IQuantityInput
        {
            public override IEnumerable<CartEntityModelBase> ChildItems { get; set; } = new List<ShoppingCartItemModel>();

            public bool DisableBuyButton { get; set; }
        }

        #endregion
    }
}