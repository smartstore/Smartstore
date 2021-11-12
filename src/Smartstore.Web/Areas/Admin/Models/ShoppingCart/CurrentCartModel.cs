using System;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.ShoppingCart
{
    public class CurrentCartListModel : ModelBase
    {
        public ShoppingCartType CartType { get; set; }
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
