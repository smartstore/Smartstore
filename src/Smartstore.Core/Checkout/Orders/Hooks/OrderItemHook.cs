using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    internal class OrderItemHook : AsyncDbSaveHook<OrderItem>
    {
        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;

        public OrderItemHook(SmartDbContext db, IEventPublisher eventPublisher)
        {
            _db = db;
            _eventPublisher = eventPublisher;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var orderItems = entries
                .Select(x => x.Entity)
                .OfType<OrderItem>()
                .ToList();

            var orderIds = orderItems.ToDistinctArray(x => x.OrderId);

            if (orderIds.Any())
            {
                var orders = await _db.Orders
                    .Where(x => orderIds.Contains(x.Id))
                    .ToListAsync(cancelToken);

                foreach (var order in orders)
                {
                    await _eventPublisher.PublishOrderUpdatedAsync(order);
                }
            }
        }
    }
}
