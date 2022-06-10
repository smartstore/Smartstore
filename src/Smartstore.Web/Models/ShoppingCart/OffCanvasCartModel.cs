namespace Smartstore.Web.Models.Cart
{
    public partial class OffCanvasCartModel : ModelBase
    {
        // Product counts
        public int CartItemsCount { get; set; }
        public int WishlistItemsCount { get; set; }
        public int CompareItemsCount { get; set; }

        // Settings
        public bool ShoppingCartEnabled { get; set; }
        public bool WishlistEnabled { get; set; }
        public bool CompareProductsEnabled { get; set; }
    }
}