using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Admin.Models.Cart
{
    public class CurrentCartListModel : ModelBase
    {
        public ShoppingCartType CartType { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int StoreId { get; set; }
    }

    [LocalizedDisplay("Admin.CurrentCarts.")]
    public class CurrentCartModel : ModelBase
    {
        [LocalizedDisplay("*Customer")]
        public int CustomerId { get; set; }

        [LocalizedDisplay("*Customer")]
        public string CustomerEmail { get; set; }

        [LocalizedDisplay("*TotalItems")]
        public int TotalItems { get; set; }

        [LocalizedDisplay("Common.Date")]
        public DateTime? LatestCartItemDate { get; set; }

        public bool IsGuest { get; set; }
        public string CustomerEditUrl { get; set; }
    }
}
