using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Customers;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Orders
{
    public class OrderMap : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);

            builder.Property(x => x.CurrencyRate).HasPrecision(18, 8);

            // TODO: (ms) (core) needs orders nav prop in customer
            //builder.HasOne(x => x.Customer)
            //    .WithMany(x => x.Orders)
            //    .HasForeignKey(x => x.CustomerId);

            builder.HasOne(o => o.BillingAddress)
                .WithMany()
                .HasForeignKey(o => o.BillingAddressId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    [Index(nameof(CustomerId), Name = "IX_Order_CustomerId")]
    public partial class Order : EntityWithAttributes, IAuditable, ISoftDeletable
    {
        private readonly ILazyLoader _lazyLoader;

        public Order()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private Order(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        #region Properties

        /// <summary>
        /// Gets or sets the (formatted) order number
        /// </summary>
        public string OrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        public Guid OrderGuid { get; set; }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the billing address identifier
        /// </summary>
        public int BillingAddressId { get; set; }

        /// <summary>
        /// Gets or sets the shipping address identifier
        /// </summary>
        public int? ShippingAddressId { get; set; }

        /// <summary>
        /// Gets or sets the payment method system name
        /// </summary>
        public string PaymentMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets the customer currency code (at the moment of order placing)
        /// </summary>
        public string CustomerCurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets the currency rate
        /// </summary>
        public decimal CurrencyRate { get; set; }

        /// <summary>
        /// Gets or sets the VAT number (the European Union Value Added Tax)
        /// </summary>
        public string VatNumber { get; set; }

        /// <summary>
        /// Gets or sets the order subtotal (incl tax)
        /// </summary>
        public decimal OrderSubtotalInclTax { get; set; }

        /// <summary>
        /// Gets or sets the order subtotal (excl tax)
        /// </summary>
        public decimal OrderSubtotalExclTax { get; set; }

        /// <summary>
        /// Gets or sets the order subtotal discount (incl tax)
        /// </summary>
        public decimal OrderSubTotalDiscountInclTax { get; set; }

        /// <summary>
        /// Gets or sets the order subtotal discount (excl tax)
        /// </summary>
        public decimal OrderSubTotalDiscountExclTax { get; set; }

        /// <summary>
        /// Gets or sets the order shipping (incl tax)
        /// </summary>
        public decimal OrderShippingInclTax { get; set; }

        /// <summary>
        /// Gets or sets the order shipping (excl tax)
        /// </summary>
        public decimal OrderShippingExclTax { get; set; }

        /// <summary>
        /// Gets or sets the tax rate for order shipping
        /// </summary>
        public decimal OrderShippingTaxRate { get; set; }

        /// <summary>
        /// Gets or sets the payment method additional fee (incl tax)
        /// </summary>
        public decimal PaymentMethodAdditionalFeeInclTax { get; set; }

        /// <summary>
        /// Gets or sets the payment method additional fee (excl tax)
        /// </summary>
        public decimal PaymentMethodAdditionalFeeExclTax { get; set; }

        /// <summary>
        /// Gets or sets the tax rate for payment method additional fee
        /// </summary>
        public decimal PaymentMethodAdditionalFeeTaxRate { get; set; }

        /// <summary>
        /// Gets or sets the tax rates
        /// </summary>
        public string TaxRates { get; set; }

        /// <summary>
        /// Gets or sets the order tax
        /// </summary>
        public decimal OrderTax { get; set; }

        /// <summary>
        /// Gets or sets the order discount (applied to order total)
        /// </summary>
        public decimal OrderDiscount { get; set; }

        /// <summary>
        /// Gets or sets the wallet credit amount used to (partially) pay this order.
        /// </summary>
        public decimal CreditBalance { get; set; }

        /// <summary>
        /// Gets or sets the order total rounding amount
        /// </summary>
        public decimal OrderTotalRounding { get; set; }

        /// <summary>
        /// Gets or sets the order total
        /// </summary>
        public decimal OrderTotal { get; set; }

        /// <summary>
        /// Gets or sets the refunded amount
        /// </summary>
        public decimal RefundedAmount { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether reward points were earned for this order
        /// </summary>
        public bool RewardPointsWereAdded { get; set; }

        /// <summary>
        /// Gets or sets the checkout attribute description
        /// </summary>
        public string CheckoutAttributeDescription { get; set; }

        /// <summary>
        /// Gets or sets the checkout attributes in XML format
        /// </summary>
        public string CheckoutAttributesXml { get; set; }

        /// <summary>
        /// Gets or sets the customer language identifier
        /// </summary>
        public int CustomerLanguageId { get; set; }

        /// <summary>
        /// Gets or sets the affiliate identifier
        /// </summary>
        public int AffiliateId { get; set; }

        /// <summary>
        /// Gets or sets the customer IP address
        /// </summary>
        public string CustomerIp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether storing of credit card number is allowed
        /// </summary>
        public bool AllowStoringCreditCardNumber { get; set; }

        /// <summary>
        /// Gets or sets the card type
        /// </summary>
        public string CardType { get; set; }

        /// <summary>
        /// Gets or sets the card name
        /// </summary>
        [JsonIgnore]
        public string CardName { get; set; }

        /// <summary>
        /// Gets or sets the card number
        /// </summary>
        [JsonIgnore]
        public string CardNumber { get; set; }

        /// <summary>
        /// Gets or sets the masked credit card number
        /// </summary>
        [JsonIgnore]
        public string MaskedCreditCardNumber { get; set; }

        /// <summary>
        /// Gets or sets the card CVV2
        /// </summary>
        [JsonIgnore]
        public string CardCvv2 { get; set; }

        /// <summary>
        /// Gets or sets the card expiration month
        /// </summary>
        [JsonIgnore]
        public string CardExpirationMonth { get; set; }

        /// <summary>
        /// Gets or sets the card expiration year
        /// </summary>
        [JsonIgnore]
        public string CardExpirationYear { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether storing of credit card number is allowed
        /// </summary>
        [JsonIgnore]
        public bool AllowStoringDirectDebit { get; set; }

        /// <summary>
        /// Gets or sets the direct debit account holder
        /// </summary>
        [JsonIgnore]
        public string DirectDebitAccountHolder { get; set; }

        /// <summary>
        /// Gets or sets the direct debit account number
        /// </summary>
        [JsonIgnore]
        public string DirectDebitAccountNumber { get; set; }

        /// <summary>
        /// Gets or sets the direct debit bank code
        /// </summary>
        [JsonIgnore]
        public string DirectDebitBankCode { get; set; }

        /// <summary>
        /// Gets or sets the direct debit bank name
        /// </summary>
        [JsonIgnore]
        public string DirectDebitBankName { get; set; }

        /// <summary>
        /// Gets or sets the direct debit bic
        /// </summary>
        [JsonIgnore]
        public string DirectDebitBIC { get; set; }

        /// <summary>
        /// Gets or sets the direct debit country
        /// </summary>
        [JsonIgnore]
        public string DirectDebitCountry { get; set; }

        /// <summary>
        /// Gets or sets the direct debit iban
        /// </summary>
        [JsonIgnore]
        public string DirectDebitIban { get; set; }

        /// <summary>
        /// Gets or sets the customer order comment
        /// </summary>
        public string CustomerOrderComment { get; set; }

        /// <summary>
        /// Gets or sets the authorization transaction identifier
        /// </summary>
        public string AuthorizationTransactionId { get; set; }

        /// <summary>
        /// Gets or sets the authorization transaction code
        /// </summary>
        public string AuthorizationTransactionCode { get; set; }

        /// <summary>
        /// Gets or sets the authorization transaction result
        /// </summary>
        public string AuthorizationTransactionResult { get; set; }

        /// <summary>
        /// Gets or sets the capture transaction identifier
        /// </summary>
        public string CaptureTransactionId { get; set; }

        /// <summary>
        /// Gets or sets the capture transaction result
        /// </summary>
        public string CaptureTransactionResult { get; set; }

        /// <summary>
        /// Gets or sets the subscription transaction identifier
        /// </summary>
        public string SubscriptionTransactionId { get; set; }

        /// <summary>
        /// Gets or sets the purchase order number
        /// </summary>
        public string PurchaseOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the paid date and time
        /// </summary>
        public DateTime? PaidDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the shipping method
        /// </summary>
        public string ShippingMethod { get; set; }

        /// <summary>
        /// Gets or sets the shipping rate computation method identifier
        /// </summary>
        public string ShippingRateComputationMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the date and time of order creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time when order was updated
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the amount of remaing reward points
        /// </summary>
        public int? RewardPointsRemaining { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a new payment notification arrived (IPN, webhook, callback etc.)
        /// </summary>
        public bool HasNewPaymentNotification { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer accepted to hand over email address to third party
        /// </summary>
        public bool AcceptThirdPartyEmailHandOver { get; set; }

        /// <summary>
        /// Gets or sets an order status identifier
        /// </summary>
        public int OrderStatusId { get; set; }

        /// <summary>
        /// Gets or sets the order status
        /// </summary>
        [NotMapped]
        public OrderStatus OrderStatus
        {
            get => (OrderStatus)OrderStatusId;
            set => OrderStatusId = (int)value;
        }

        /// <summary>
        /// Gets or sets the payment status identifier
        /// </summary>
        public int PaymentStatusId { get; set; }

        /// <summary>
        /// Gets or sets the payment status
        /// </summary>
        [NotMapped]
        public PaymentStatus PaymentStatus
        {
            get => (PaymentStatus)PaymentStatusId;
            set => PaymentStatusId = (int)value;
        }

        /// <summary>
        /// Gets or sets the shipping status identifier
        /// </summary>
        public int ShippingStatusId { get; set; }

        /// <summary>
        /// Gets or sets the shipping status
        /// </summary>
        [NotMapped]
        public ShippingStatus ShippingStatus
        {
            get => (ShippingStatus)ShippingStatusId;
            set => ShippingStatusId = (int)value;
        }

        /// <summary>
        /// Gets or sets the customer tax display type identifier
        /// </summary>
        public virtual int CustomerTaxDisplayTypeId { get; set; }

        /// <summary>
        /// Gets or sets the customer tax display type
        /// </summary>
        [NotMapped]
        public TaxDisplayType CustomerTaxDisplayType
        {
            get => (TaxDisplayType)CustomerTaxDisplayTypeId;
            set => CustomerTaxDisplayTypeId = (int)value;
        }

        #endregion

        #region Navigation properties

        private Customer _customer;
        /// <summary>
        /// Gets or sets the customer
        /// </summary>
        public Customer Customer
        {
            get => _lazyLoader?.Load(this, ref _customer) ?? _customer;
            set => _customer = value;
        }

        private Address _billingAddress;
        /// <summary>
        /// Gets or sets the billing address
        /// </summary>
        public Address BillingAddress
        {
            get => _lazyLoader?.Load(this, ref _billingAddress) ?? _billingAddress;
            set => _billingAddress = value;
        }

        private Address _shippingAddress;
        /// <summary>
        /// Gets or sets the shipping address
        /// </summary>
        public Address ShippingAddress
        {
            get => _lazyLoader?.Load(this, ref _shippingAddress) ?? _shippingAddress;
            set => _shippingAddress = value;
        }

        /// <summary>
        /// Gets or sets the reward points history record
        /// </summary>
        // TODO: (ms) (core) needs RewardPointsHistory of customer
        //[JsonIgnore]
        //public RewardPointsHistory RedeemedRewardPointsEntry { get; set; }

        /// <summary>
        /// Gets or sets the wallet history.
        /// </summary>
        // TODO: (ms) (core) needs WalletHistory of customer
        //[JsonIgnore]
        //public ICollection<WalletHistory> WalletHistory
        //{
        //    get => _walletHistory ?? (_walletHistory = new HashSet<WalletHistory>());
        //    protected set => _walletHistory = value;
        //}

        /// <summary>
        /// Gets or sets discount usage history
        /// </summary>
        // TODO: (ms) (core) needs DiscountUsageHistory of catalog
        //[JsonIgnore]
        //public ICollection<DiscountUsageHistory> DiscountUsageHistory
        //{
        //    get => _discountUsageHistory ?? (_discountUsageHistory = new HashSet<DiscountUsageHistory>());
        //    protected set => _discountUsageHistory = value;
        //}

        private ICollection<GiftCardUsageHistory> _giftCardUsageHistory;
        /// <summary>
        /// Gets or sets gift card usage history (gift card that were used with this order)
        /// </summary>
        [JsonIgnore]
        public ICollection<GiftCardUsageHistory> GiftCardUsageHistory
        {
            get => _lazyLoader?.Load(this, ref _giftCardUsageHistory) ?? (_giftCardUsageHistory ??= new HashSet<GiftCardUsageHistory>());
            protected set => _giftCardUsageHistory = value;
        }

        private ICollection<OrderNote> _orderNotes;
        /// <summary>
        /// Gets or sets order notes
        /// </summary>
        public ICollection<OrderNote> OrderNotes
        {
            get => _lazyLoader?.Load(this, ref _orderNotes) ?? (_orderNotes ??= new HashSet<OrderNote>());
            protected set => _orderNotes = value;
        }

        private ICollection<OrderItem> _orderItems;
        /// <summary>
        /// Gets or sets order items
        /// </summary>
        [JsonIgnore]
        public ICollection<OrderItem> OrderItems
        {
            get => _lazyLoader?.Load(this, ref _orderItems) ?? (_orderItems ??= new HashSet<OrderItem>());
            protected internal set => _orderItems = value;
        }

        private ICollection<Shipment> _shipments;
        /// <summary>
        /// Gets or sets shipments
        /// </summary>
        public ICollection<Shipment> Shipments
        {
            get => _lazyLoader?.Load(this, ref _shipments) ?? (_shipments ??= new HashSet<Shipment>());
            protected set => _shipments = value;
        }

        #endregion

        /// <summary>
        /// Gets the applied tax rates
        /// </summary>
        [NotMapped]
        public SortedDictionary<decimal, decimal> TaxRatesDictionary => ParseTaxRates(TaxRates);

        protected static SortedDictionary<decimal, decimal> ParseTaxRates(string taxRatesStr)
        {
            var taxRates = new SortedDictionary<decimal, decimal>();
            if (!taxRatesStr.HasValue())
                return taxRates;

            var lines = taxRatesStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (!line.Trim().HasValue())
                    continue;

                var taxes = line.Split(':');
                if (taxes.Length != 2)
                    continue;

                try
                {
                    var taxRate = decimal.Parse(taxes[0].Trim(), CultureInfo.InvariantCulture);
                    var taxValue = decimal.Parse(taxes[1].Trim(), CultureInfo.InvariantCulture);
                    taxRates.Add(taxRate, taxValue);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            // Have at least one tax rate (0%)
            if (taxRates.Count == 0)
            {
                taxRates.Add(decimal.Zero, decimal.Zero);
            }

            return taxRates;
        }
    }
}