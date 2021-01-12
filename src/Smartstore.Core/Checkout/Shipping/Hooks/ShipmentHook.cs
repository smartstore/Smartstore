using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Shipping.Hooks
{
    /// <summary>
    /// Shipment hook
    /// </summary>
    [Important]
    public partial class ShipmentHook : AsyncDbSaveHook<Shipment>
    {
        private readonly IEventPublisher _eventPublisher;

        public ShipmentHook(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        //public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        //{
        //    if (entry.InitialState == Smartstore.Data.EntityState.Deleted)
        //    {

        //    }
        //    else
        //    {

        //    }
        //}

        protected override Task<HookResult> OnInsertingAsync(Shipment entity, IHookedEntity entry, CancellationToken cancelToken) 
            => PublishOrderUpdatedAsync(entity.Order);

        protected override Task<HookResult> OnUpdatingAsync(Shipment entity, IHookedEntity entry, CancellationToken cancelToken) 
            => PublishOrderUpdatedAsync(entity.Order);

        protected override Task<HookResult> OnDeletingAsync(Shipment entity, IHookedEntity entry, CancellationToken cancelToken) 
            => PublishOrderUpdatedAsync(entity.Order);

        private async Task<HookResult> PublishOrderUpdatedAsync(Order order)
        {
            await _eventPublisher.PublishOrderUpdatedAsync(order);

            return HookResult.Ok;
        }
    }

    /// <summary>
    /// Shipment item hook
    /// </summary>
    [Important]
    public partial class ShipmentItemHook : AsyncDbSaveHook<ShipmentItem>
    {
        private readonly IEventPublisher _eventPublisher;

        public ShipmentItemHook(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        private Task<HookResult> PublishOrderUpdatedAsync(Order order)
        {
            _eventPublisher.PublishOrderUpdatedAsync(order);

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnInsertingAsync(ShipmentItem entity, IHookedEntity entry, CancellationToken cancelToken)
            => PublishOrderUpdatedAsync(entity.Shipment?.Order);

        protected override Task<HookResult> OnUpdatingAsync(ShipmentItem entity, IHookedEntity entry, CancellationToken cancelToken)
            => PublishOrderUpdatedAsync(entity.Shipment?.Order);

        protected override Task<HookResult> OnDeletingAsync(ShipmentItem entity, IHookedEntity entry, CancellationToken cancelToken)
            => PublishOrderUpdatedAsync(entity.Shipment?.Order);
    }
}
