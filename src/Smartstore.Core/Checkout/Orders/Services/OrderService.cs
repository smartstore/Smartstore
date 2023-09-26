using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderService : AsyncDbSaveHook<Order>, IOrderService
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly Lazy<IOrderProcessingService> _orderProcessingService;
        private readonly IRoundingHelper _roundingHelper;
        private readonly IEventPublisher _eventPublisher;
        private readonly PaymentSettings _paymentSettings;
        private readonly HashSet<Order> _toCapture = new();

        public OrderService(
            SmartDbContext db,
            IWorkContext workContext,
            Lazy<IOrderProcessingService> orderProcessingService,
            IRoundingHelper roundingHelper,
            IEventPublisher eventPublisher,
            PaymentSettings paymentSettings)
        {
            _db = db;
            _workContext = workContext;
            _orderProcessingService = orderProcessingService;
            _roundingHelper = roundingHelper;
            _eventPublisher = eventPublisher;
            _paymentSettings = paymentSettings;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Hook

        protected override Task<HookResult> OnUpdatingAsync(Order entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Check whether to automatically capture payment.
            if (_paymentSettings.CapturePaymentReason.HasValue)
            {
                var prop = entry.Entry.Property(nameof(entity.ShippingStatusId));
                var equalValue = prop?.CurrentValue?.Equals(prop.OriginalValue) ?? true;
                if (!equalValue)
                {
                    var newStatus = (ShippingStatus)(int)prop.CurrentValue;
                    var reason = _paymentSettings.CapturePaymentReason.Value;

                    // INFO: CapturePaymentReason.OrderCompleted is processed in CompleteOrderAsync. Would be too late here
                    // because the payment would already be marked as paid and capturing never happens.
                    if ((newStatus == ShippingStatus.Shipped && reason == CapturePaymentReason.OrderShipped) ||
                        (newStatus == ShippingStatus.Delivered && reason == CapturePaymentReason.OrderDelivered))
                    {
                        _toCapture.Add(entity);
                    }
                }
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatedAsync(Order entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var orders = entries
                .Select(x => x.Entity)
                .OfType<Order>()
                .ToList();

            foreach (var order in orders)
            {
                await _eventPublisher.PublishOrderUpdatedAsync(order);
            }

            // Automatically capture payments.
            if (_toCapture.Any())
            {
                var numErrors = 0;
                foreach (var order in _toCapture)
                {
                    if (await _orderProcessingService.Value.CanCaptureAsync(order))
                    {
                        try
                        {
                            await _orderProcessingService.Value.CaptureAsync(order);
                        }
                        catch (Exception ex)
                        {
                            if (++numErrors <= 3)
                            {
                                Logger.Error(ex);
                            }
                        }
                    }
                }

                _toCapture.Clear();
            }
        }

        #endregion

        public async Task<(Money OrderTotal, Money RoundingAmount)> GetOrderTotalInCustomerCurrencyAsync(Order order, Currency targetCurrency)
        {
            Guard.NotNull(order);

            var roundingAmount = order.OrderTotalRounding;
            var orderTotal = order.OrderTotal * order.CurrencyRate;

            // Avoid rounding a rounded value. It would zero roundingAmount.
            if (orderTotal != order.OrderTotal &&
                targetCurrency != null &&
                targetCurrency.RoundOrderTotalEnabled &&
                order.PaymentMethodSystemName.HasValue())
            {
                var paymentMethod = await _db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentMethodSystemName == order.PaymentMethodSystemName);
                if (paymentMethod?.RoundOrderTotalEnabled ?? false)
                {
                    orderTotal = _roundingHelper.ToNearest(orderTotal, out roundingAmount, targetCurrency);
                }
            }

            // Currency for output.
            var currency = targetCurrency ?? _workContext.WorkingCurrency;

            return (new(orderTotal, currency), new(roundingAmount, currency));
        }
    }
}
