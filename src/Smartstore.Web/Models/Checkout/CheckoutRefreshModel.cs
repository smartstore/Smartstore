using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Web.Models.Checkout;

public class CheckoutRefreshModel : CheckoutModelBase
{
    public CheckoutPartial Parts { get; set; }

    public string PaymentMethodSystemName { get; set; }

    public string ShippingOption { get; set; }
}
