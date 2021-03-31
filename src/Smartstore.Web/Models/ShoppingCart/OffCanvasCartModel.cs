using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.ShoppingCart
{
    public partial class OffCanvasCartModel : ModelBase
    {
        // product counts
        public int CartItemsCount { get; set; }
        public int WishlistItemsCount { get; set; }
        public int CompareItemsCount { get; set; }

        // settings
        public bool ShoppingCartEnabled { get; set; }
        public bool WishlistEnabled { get; set; }
        public bool CompareProductsEnabled { get; set; }
    }
}
