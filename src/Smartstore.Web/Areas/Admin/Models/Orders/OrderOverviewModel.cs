using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Fields.")]
    public class OrderOverviewModel : TabbableModel
    {
        [LocalizedDisplay("*ID")]
        public override int Id { get; set; }

        [LocalizedDisplay("*OrderNumber")]
        public string OrderNumber { get; set; }

        [LocalizedDisplay("*OrderGuid")]
        public Guid OrderGuid { get; set; }

        [LocalizedDisplay("*Store")]
        public string StoreName { get; set; }
        public string FromStore { get; set; }

        [LocalizedDisplay("*Customer")]
        public int CustomerId { get; set; }

        [LocalizedDisplay("Admin.Orders.List.CustomerName")]
        public string CustomerName { get; set; }

        [LocalizedDisplay("*CustomerEmail")]
        public string CustomerEmail { get; set; }

        [LocalizedDisplay("*VatNumber")]
        public string VatNumber { get; set; }

        [LocalizedDisplay("*OrderTotal")]
        public string OrderTotalString { get; set; }

        [LocalizedDisplay("*OrderStatus")]
        public OrderStatus OrderStatus { get; set; }
        public string OrderStatusString { get; set; }

        public string OrderStatusLabelClass
        {
            get
            {
                return OrderStatus switch
                {
                    OrderStatus.Pending => "fw-600",
                    OrderStatus.Processing => "",
                    OrderStatus.Complete => "text-success",
                    OrderStatus.Cancelled => "muted",
                    _ => string.Empty,
                };
            }
        }

        [LocalizedDisplay("*PaymentStatus")]
        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentStatusString { get; set; }

        public string PaymentStatusLabelClass
        {
            get
            {
                return PaymentStatus switch
                {
                    PaymentStatus.Pending => "fa fa-fw fa-circle text-danger",
                    PaymentStatus.Authorized => "fa fa-fw fa-circle text-warning",
                    PaymentStatus.Paid => "fa fa-fw fa-check text-success",
                    PaymentStatus.PartiallyRefunded => "fa fa-fw fa-exchange-alt text-warning",
                    PaymentStatus.Refunded => "fa fa-fw fa-exchange-alt text-success",
                    PaymentStatus.Voided => "fa fa-fw fa-ban muted",
                    _ => string.Empty,
                };
            }
        }

        [LocalizedDisplay("*PaymentMethod")]
        public string PaymentMethod { get; set; }
        public string PaymentMethodSystemName { get; set; }
        public string WithPaymentMethod { get; set; }

        public bool HasPaymentMethod
            => PaymentMethod.HasValue();
        public bool HasNewPaymentNotification { get; set; }

        [LocalizedDisplay("*ShippingStatus")]
        public ShippingStatus ShippingStatus { get; set; }
        public string ShippingStatusString { get; set; }
        public string ShippingAddressString { get; set; }

        public bool IsShippable
            => ShippingStatus != ShippingStatus.ShippingNotRequired;

        public string ShippingStatusLabelClass
        {
            get
            {
                return ShippingStatus switch
                {
                    ShippingStatus.ShippingNotRequired => "fa fa-fw fa-download muted",
                    ShippingStatus.NotYetShipped => "fa fa-fw fa-circle text-danger",
                    ShippingStatus.PartiallyShipped => "fa fa-fw fa-truck fa-flip-horizontal text-warning",
                    ShippingStatus.Shipped => "fa fa-fw fa-truck fa-flip-horizontal text-success",
                    ShippingStatus.Delivered => "fa fa-fw fa-check text-success",
                    _ => String.Empty,
                };
            }
        }

        [LocalizedDisplay("*ShippingMethod")]
        public string ShippingMethod { get; set; }
        public string ViaShippingMethod { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }
        public string CreatedOnString
            => CreatedOn.ToString("g");

        [LocalizedDisplay("Common.UpdatedOn")]
        public DateTime UpdatedOn { get; set; }
        public string UpdatedOnString
            => UpdatedOn.ToString("g");

        public string EditUrl { get; set; }
        public string CustomerEditUrl { get; set; }
    }
}
