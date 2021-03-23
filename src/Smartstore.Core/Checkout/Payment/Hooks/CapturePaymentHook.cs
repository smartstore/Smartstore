using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Checkout.Payment.Hooks
{
    public class CapturePaymentHook : AsyncDbSaveHook<Order>
    {
        private readonly ISettingFactory _settingFactory;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly HashSet<Order> _toCapture = new();

        public CapturePaymentHook(
            ISettingFactory settingFactory,
            IOrderProcessingService orderProcessingService)
        {
            _settingFactory = settingFactory;
            _orderProcessingService = orderProcessingService;
        }

        private static bool IsStatusPropertyModifiedTo(IHookedEntity entry, string propertyName, int statusId)
        {
            var prop = entry.Entry.Property(propertyName);
            if (prop?.CurrentValue != null)
            {
                if (!prop.CurrentValue.Equals(prop.OriginalValue))
                {
                    return (int)prop.CurrentValue == statusId;
                }
            }

            return false;
        }

        protected override async Task<HookResult> OnUpdatingAsync(Order entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var isShipped = IsStatusPropertyModifiedTo(entry, nameof(entity.ShippingStatusId), (int)ShippingStatus.Shipped);
            var isDelivered = IsStatusPropertyModifiedTo(entry, nameof(entity.ShippingStatusId), (int)ShippingStatus.Delivered);

            if (isShipped || isDelivered)
            {
                var settings = await _settingFactory.LoadSettingsAsync<PaymentSettings>(entity.StoreId);
                if (settings.CapturePaymentReason.HasValue)
                {
                    if (isShipped && settings.CapturePaymentReason.Value == CapturePaymentReason.OrderShipped)
                    {
                        _toCapture.Add(entity);
                    }
                    else if (isDelivered && settings.CapturePaymentReason.Value == CapturePaymentReason.OrderDelivered)
                    {
                        _toCapture.Add(entity);
                    }
                }
            }

            return HookResult.Ok;

            //if (IsStatusPropertyModifiedTo(entry, nameof(entity.OrderStatusId), (int)OrderStatus.Complete))
            //{
            // That's too late. The payment is already marked as paid and the capture process would never be executed.
            //}
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            // Do not remove.
            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_toCapture.Any())
            {
                foreach (var order in _toCapture)
                {
                    if (await _orderProcessingService.CanCaptureAsync(order))
                    {
                        await _orderProcessingService.CaptureAsync(order);
                    }
                }

                _toCapture.Clear();
            }
        }
    }
}