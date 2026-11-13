using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Admin.Models.Orders;

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
    public bool CustomerDeleted { get; set; }

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
                OrderStatus.Cancelled => "text-muted",
                _ => string.Empty,
            };
        }
    }

    [LocalizedDisplay("*PaymentStatus")]
    public PaymentStatus PaymentStatus { get; set; }
    public string PaymentStatusString { get; set; }
    public bool IsPaid
        => PaymentStatus == PaymentStatus.Paid;

    /// <summary>
    /// Gets the icon concept for the payment status.
    /// </summary>
    [LocalizedDisplay("*PaymentStatus")]
    public string PaymentStatusIcon => PaymentStatus switch
    {
        PaymentStatus.Pending or PaymentStatus.Authorized => "circle-fill",
        PaymentStatus.Paid => "check",
        PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded => "refund",
        PaymentStatus.Voided => "ban",
        _ => null
    };

    public string PaymentStatusLabelClass
    {
        get
        {
            return PaymentStatus switch
            {
                PaymentStatus.Pending => "text-danger",
                PaymentStatus.Authorized => "text-warning",
                PaymentStatus.Paid => "text-success",
                PaymentStatus.PartiallyRefunded => "text-warning",
                PaymentStatus.Refunded => "text-success",
                PaymentStatus.Voided => "text-muted",
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

    /// <summary>
    /// Gets the icon concept for the shipping status.
    /// </summary>
    [LocalizedDisplay("*ShippingStatus")]
    public string ShippingStatusIcon => ShippingStatus switch
    {
        ShippingStatus.ShippingNotRequired => "download",
        ShippingStatus.NotYetShipped => "circle-fill",
        ShippingStatus.PartiallyShipped => "truck",
        ShippingStatus.Shipped => "shipped",
        ShippingStatus.Delivered => "check",
        _ => null
    };

    public string ShippingStatusLabelClass
    {
        get
        {
            return ShippingStatus switch
            {
                ShippingStatus.ShippingNotRequired => "text-muted mr-1",
                ShippingStatus.NotYetShipped => "text-danger mr-1",
                ShippingStatus.PartiallyShipped => "flip-h text-warning mr-1",
                ShippingStatus.Shipped => "flip-h text-success mr-1",
                ShippingStatus.Delivered => "text-success mr-1",
                _ => string.Empty,
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