using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Models.Cart
{
    public partial class WishlistModel : CartModelBase
    {
        public override IEnumerable<WishlistItemModel> Items { get; } = new List<WishlistItemModel>();

        public void AddItems(params WishlistItemModel[] models)
        {
            ((List<WishlistItemModel>)Items).AddRange(models);
        }

        public Guid CustomerGuid { get; set; }
        public string CustomerFullname { get; set; }
        public bool DisplayAddToCart { get; set; }
        public bool EmailWishlistEnabled { get; set; }
        public int ThumbSize { get; set; }
        public bool ShowItemsFromWishlistToCartButton { get; set; }

        public partial class WishlistItemModel : CartEntityModelBase, IQuantityInput
        {
            public override IEnumerable<WishlistItemModel> ChildItems { get; } = new List<WishlistItemModel>();

            public void AddChildItems(params WishlistItemModel[] models)
            {
                ((List<WishlistItemModel>)ChildItems).AddRange(models);
            }

            public bool DisableBuyButton { get; set; }
        }
    }
}