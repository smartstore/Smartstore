using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders
{
    internal class OrderMap : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);

            builder.Property(x => x.CurrencyRate).HasPrecision(18, 8);

            builder
                .HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId);

            // INFO: DeleteBehavior.ClientSetNull instead of DeleteBehavior.SetNull required because of cycles or multiple cascade paths.
            // This is of little importance anyway because both addresses are cloned when the order is created and are never deleted afterwards.
            builder
                .HasOne(o => o.BillingAddress)
                .WithMany()
                .HasForeignKey(o => o.BillingAddressId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder
                .HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    [Index(nameof(Deleted), Name = "IX_Deleted")]
    [Index(nameof(CustomerId), Name = "IX_Order_CustomerId")]
    [Index(nameof(OrderGuid))]
    // INFO: MySQL max key length is 3072 bytes, so we can index up to 768 characters for utf8mb4.
    [Index(nameof(PaymentMethodSystemName), nameof(AuthorizationTransactionId))]
    [Index(nameof(PaymentMethodSystemName), nameof(AuthorizationTransactionCode))]
    [Index(nameof(PaymentMethodSystemName), nameof(CaptureTransactionId))]
    public partial class Order : EntityWithAttributes, IAuditable, ISoftDeletable
    {
        #region Properties

        /// <summary>
        /// Gets or sets the (formatted) order number
        /// </summary>
        [StringLength(400)]
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
        public int? BillingAddressId { get; set; }

        /// <summary>
        /// Gets or sets the shipping address identifier
        /// </summary>
        public int? ShippingAddressId { get; set; }

        /// <summary>
        /// Gets or sets the payment method system name
        /// </summary>
        [StringLength(255)]
        public string PaymentMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets the customer currency code (at the moment of order placing)
        /// </summary>
        [StringLength(5)]
        public string CustomerCurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets the currency rate
        /// </summary>
        public decimal CurrencyRate { get; set; }

        /// <summary>
        /// Gets or sets the VAT number (the European Union Value Added Tax)
        /// </summary>
        [StringLength(400)]
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
        [StringLength(4000)]
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
        /// Gets or sets the checkout attributes in XML or JSON format
        /// </summary>
        [Column("CheckoutAttributesXml")]
        public string RawAttributes { get; set; }

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
        [StringLength(200)]
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
        [IgnoreDataMember]
        public string CardName { get; set; }

        /// <summary>
        /// Gets or sets the card number
        /// </summary>
        [IgnoreDataMember]
        public string CardNumber { get; set; }

        /// <summary>
        /// Gets or sets the masked credit card number
        /// </summary>
        [IgnoreDataMember]
        public string MaskedCreditCardNumber { get; set; }

        /// <summary>
        /// Gets or sets the card CVV2
        /// </summary>
        [IgnoreDataMember]
        public string CardCvv2 { get; set; }

        /// <summary>
        /// Gets or sets the card expiration month
        /// </summary>
        [IgnoreDataMember]
        public string CardExpirationMonth { get; set; }

        /// <summary>
        /// Gets or sets the card expiration year
        /// </summary>
        [IgnoreDataMember]
        public string CardExpirationYear { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether storing of credit card number is allowed
        /// </summary>
        [IgnoreDataMember]
        public bool AllowStoringDirectDebit { get; set; }

        /// <summary>
        /// Gets or sets the direct debit account holder
        /// </summary>
        [IgnoreDataMember]
        public string DirectDebitAccountHolder { get; set; }

        /// <summary>
        /// Gets or sets the direct debit account number
        /// </summary>
        [IgnoreDataMember]
        public string DirectDebitAccountNumber { get; set; }

        /// <summary>
        /// Gets or sets the direct debit bank code
        /// </summary>
        [IgnoreDataMember]
        public string DirectDebitBankCode { get; set; }

        /// <summary>
        /// Gets or sets the direct debit bank name
        /// </summary>
        [IgnoreDataMember]
        public string DirectDebitBankName { get; set; }

        /// <summary>
        /// Gets or sets the direct debit bic
        /// </summary>
        [IgnoreDataMember]
        public string DirectDebitBIC { get; set; }

        /// <summary>
        /// Gets or sets the direct debit country
        /// </summary>
        [IgnoreDataMember]
        public string DirectDebitCountry { get; set; }

        /// <summary>
        /// Gets or sets the direct debit iban
        /// </summary>
        [IgnoreDataMember]
        public string DirectDebitIban { get; set; }

        /// <summary>
        /// Gets or sets the customer order comment
        /// </summary>
        [MaxLength]
        public string CustomerOrderComment { get; set; }

        /// <summary>
        /// Gets or sets the ID of a payment authorization.
        /// Usually this comes from a payment gateway.
        /// </summary>
        [StringLength(400)]
        public string AuthorizationTransactionId { get; set; }

        /// <summary>
        /// Gets or sets a payment transaction code.
        /// Not used by Smartstore. Can be any data that the payment provider needs for later processing.
        /// Use <see cref="GenericAttribute"/> if you want to store even more payment data for an order.
        /// </summary>
        [StringLength(400)]
        public string AuthorizationTransactionCode { get; set; }

        /// <summary>
        /// Gets or sets a short result info about the payment authorization.
        /// </summary>
        [StringLength(400)]
        public string AuthorizationTransactionResult { get; set; }

        /// <summary>
        /// Gets or sets the ID of a payment capture.
        /// Usually this comes from a payment gateway. Can be equal to <see cref="AuthorizationTransactionId"/>.
        /// </summary>
        [StringLength(400)]
        public string CaptureTransactionId { get; set; }

        /// <summary>
        /// Gets or sets a short result info about the payment capture.
        /// </summary>
        [StringLength(400)]
        public string CaptureTransactionResult { get; set; }

        /// <summary>
        /// Gets or sets the ID for payment subscription. Usually used for recurring payment.
        /// </summary>
        [StringLength(400)]
        public string SubscriptionTransactionId { get; set; }

        /// <summary>
        /// Gets or sets the purchase order number
        /// </summary>
        [StringLength(400)]
        public string PurchaseOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the paid date and time
        /// </summary>
        public DateTime? PaidDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the shipping method
        /// </summary>
        [StringLength(400)]
        public string ShippingMethod { get; set; }

        /// <summary>
        /// Gets or sets the shipping rate computation method identifier
        /// </summary>
        [StringLength(400)]
        public string ShippingRateComputationMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the amount of remaing reward points
        /// </summary>
        public int? RewardPointsRemaining { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a new payment notification arrived.
        /// Set this to <c>true</c> if a new IPN, webhook notification or payment provider message arrived.
        /// Use an <see cref="OrderNote"/> to save the notification.
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
            get => _customer ?? LazyLoader.Load(this, ref _customer);
            set => _customer = value;
        }

        private Address _billingAddress;
        /// <summary>
        /// Gets or sets the billing address
        /// </summary>
        public Address BillingAddress
        {
            get => _billingAddress ?? LazyLoader.Load(this, ref _billingAddress);
            set => _billingAddress = value;
        }

        private Address _shippingAddress;
        /// <summary>
        /// Gets or sets the shipping address
        /// </summary>
        public Address ShippingAddress
        {
            get => _shippingAddress ?? LazyLoader.Load(this, ref _shippingAddress);
            set => _shippingAddress = value;
        }

        /// <summary>
        /// Gets or sets the reward points history record.
        /// </summary>
        public RewardPointsHistory RedeemedRewardPointsEntry { get; set; }

        private ICollection<WalletHistory> _walletHistory;
        /// <summary>
        /// Gets or sets the wallet history.
        /// </summary>
        public ICollection<WalletHistory> WalletHistory
        {
            get => _walletHistory ?? LazyLoader.Load(this, ref _walletHistory) ?? (_walletHistory ??= new HashSet<WalletHistory>());
            protected set => _walletHistory = value;
        }

        private ICollection<DiscountUsageHistory> _discountUsageHistory;
        /// <summary>
        /// Gets or sets discount usage history
        /// </summary>
        public ICollection<DiscountUsageHistory> DiscountUsageHistory
        {
            get => _discountUsageHistory ?? LazyLoader.Load(this, ref _discountUsageHistory) ?? (_discountUsageHistory ??= new HashSet<DiscountUsageHistory>());
            protected set => _discountUsageHistory = value;
        }

        private ICollection<GiftCardUsageHistory> _giftCardUsageHistory;
        /// <summary>
        /// Gets or sets gift card usage history (gift card that were used with this order)
        /// </summary>
        public ICollection<GiftCardUsageHistory> GiftCardUsageHistory
        {
            get => _giftCardUsageHistory ?? LazyLoader.Load(this, ref _giftCardUsageHistory) ?? (_giftCardUsageHistory ??= new HashSet<GiftCardUsageHistory>());
            protected set => _giftCardUsageHistory = value;
        }

        private ICollection<OrderNote> _orderNotes;
        /// <summary>
        /// Gets or sets order notes
        /// </summary>
        public ICollection<OrderNote> OrderNotes
        {
            get => _orderNotes ?? LazyLoader.Load(this, ref _orderNotes) ?? (_orderNotes ??= new HashSet<OrderNote>());
            protected set => _orderNotes = value;
        }

        private ICollection<OrderItem> _orderItems;
        /// <summary>
        /// Gets or sets order items
        /// </summary>
        public ICollection<OrderItem> OrderItems
        {
            get => _orderItems ?? LazyLoader.Load(this, ref _orderItems) ?? (_orderItems ??= new HashSet<OrderItem>());
            protected internal set => _orderItems = value;
        }

        private ICollection<Shipment> _shipments;
        /// <summary>
        /// Gets or sets shipments
        /// </summary>
        public ICollection<Shipment> Shipments
        {
            get => _shipments ?? LazyLoader.Load(this, ref _shipments) ?? (_shipments ??= new HashSet<Shipment>());
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