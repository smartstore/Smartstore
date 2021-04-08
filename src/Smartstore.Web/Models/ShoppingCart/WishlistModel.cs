using System;
using System.Collections.Generic;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Models.ShoppingCart
{
    public partial class WishlistModel : CartModelBase
    {
        public override IEnumerable<WishlistItemModel> Items { get; } = new List<WishlistItemModel>();

        public Guid CustomerGuid { get; set; }
        public string CustomerFullname { get; set; }
        public bool DisplayAddToCart { get; set; }
        public bool EmailWishlistEnabled { get; set; }
        public int ThumbSize { get; set; }
        public bool ShowItemsFromWishlistToCartButton { get; set; }

        #region Nested Classes

        public partial class WishlistItemModel : CartEntityModelBase, IQuantityInput
        {
            public override IEnumerable<WishlistItemModel> ChildItems { get; } = new List<WishlistItemModel>();

            public bool DisableBuyButton { get; set; }
        }

        #endregion
    }
}