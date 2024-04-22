using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Shipping.Hooks
{
    /// <summary>
    /// Shipment hook
    /// </summary>
    [Important]
    internal partial class ShipmentHook : AsyncDbSaveHook<Shipment>
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly SmartDbContext _db;

        public ShipmentHook(IEventPublisher eventPublisher, SmartDbContext db)
        {
            _eventPublisher = eventPublisher;
            _db = db;
        }

        public override async Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entry.TryGetModifiedProperty(nameof(Shipment.TrackingNumber), out var originalValue))
            {
                await _eventPublisher.PublishTrackingNumberChangedAsync(entry.Entity as Shipment, (string)originalValue);
            }

            return HookResult.Ok;
        }

        protected override async Task<HookResult> OnInsertedAsync(Shipment entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await _eventPublisher.PublishOrderUpdatedAsync(entity.Order);

            if (entity.TrackingNumber.HasValue())
            {
                await _eventPublisher.PublishTrackingNumberAddedAsync(entity);
            }

            return HookResult.Ok;
        }

        protected override async Task<HookResult> OnUpdatedAsync(Shipment entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await _eventPublisher.PublishOrderUpdatedAsync(entity.Order);

            return HookResult.Ok;
        }

        protected override async Task<HookResult> OnDeletedAsync(Shipment entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var order = await _db.Orders.FindByIdAsync(entity.OrderId, cancellationToken: cancelToken);
            if (order != null)
            {
                await _eventPublisher.PublishOrderUpdatedAsync(order);
            }

            return HookResult.Ok;
        }
    }

    /// <summary>
    /// Shipment item hook
    /// </summary>
    [Important]
    internal partial class ShipmentItemHook : AsyncDbSaveHook<ShipmentItem>
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly SmartDbContext _db;

        public ShipmentItemHook(IEventPublisher eventPublisher, SmartDbContext db)
        {
            _eventPublisher = eventPublisher;
            _db = db;
        }

        protected override async Task<HookResult> OnInsertingAsync(ShipmentItem entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await _eventPublisher.PublishOrderUpdatedAsync(entity.Shipment.Order);
            return HookResult.Ok;
        }

        protected override async Task<HookResult> OnUpdatingAsync(ShipmentItem entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await _eventPublisher.PublishOrderUpdatedAsync(entity.Shipment.Order);
            return HookResult.Ok;
        }

        protected override async Task<HookResult> OnDeletingAsync(ShipmentItem entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var order = await _db.Orders.FindByIdAsync(entity.Shipment.OrderId, cancellationToken: cancelToken);
            if (order != null)
            {
                await _eventPublisher.PublishOrderUpdatedAsync(order);
            }

            return HookResult.Ok;
        }
    }
}
