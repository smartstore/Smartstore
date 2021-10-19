using System;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Web.Modelling;

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

        [LocalizedDisplay("*OrderTotal")]
        public string OrderTotalString { get; set; }

        [LocalizedDisplay("*OrderStatus")]
        public OrderStatus OrderStatus { get; set; }
        [LocalizedDisplay("*OrderStatus")]
        public string OrderStatusString { get; set; }

        [LocalizedDisplay("*PaymentStatus")]
        public PaymentStatus PaymentStatus { get; set; }
        [LocalizedDisplay("*PaymentStatus")]
        public string PaymentStatusString { get; set; }

        [LocalizedDisplay("*PaymentMethod")]
        public string PaymentMethod { get; set; }
        public string PaymentMethodSystemName { get; set; }
        public string WithPaymentMethod { get; set; }

        public bool HasPaymentMethod => PaymentMethod.HasValue();
        public bool HasNewPaymentNotification { get; set; }

        [LocalizedDisplay("*ShippingStatus")]
        public ShippingStatus StatusShipping { get; set; }
        [LocalizedDisplay("*ShippingStatus")]
        public string ShippingStatusString { get; set; }
        public bool IsShippable { get; set; }

        [LocalizedDisplay("*ShippingMethod")]
        public string ShippingMethod { get; set; }
        public string ViaShippingMethod { get; set; }

        public string ShippingAddressString { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }
        [LocalizedDisplay("Common.CreatedOn")]
        public string CreatedOnString { get; set; }


        //...
    }
}
