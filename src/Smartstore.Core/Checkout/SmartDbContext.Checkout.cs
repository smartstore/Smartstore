using Smartstore.Core.Checkout.Affiliates;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderNote> OrderNotes { get; set; }
        public DbSet<TaxCategory> TaxCategories { get; set; }
        public DbSet<GiftCard> GiftCards { get; set; }
        public DbSet<GiftCardUsageHistory> GiftCardUsageHistory { get; set; }
        public DbSet<Affiliate> Affiliates { get; set; }
        public DbSet<CheckoutAttribute> CheckoutAttributes { get; set; }
        public DbSet<CheckoutAttributeValue> CheckoutAttributeValues { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentItem> ShipmentItems { get; set; }
        public DbSet<ShippingMethod> ShippingMethods { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<RecurringPayment> RecurringPayments { get; set; }
        public DbSet<RecurringPaymentHistory> RecurringPaymentHistory { get; set; }
    }
}