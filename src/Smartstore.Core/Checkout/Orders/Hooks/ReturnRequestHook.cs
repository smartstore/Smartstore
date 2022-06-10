using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    internal class ReturnRequestHook : AsyncDbSaveHook<ReturnRequest>
    {
        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;

        public ReturnRequestHook(SmartDbContext db, IEventPublisher eventPublisher)
        {
            _db = db;
            _eventPublisher = eventPublisher;
        }

        protected override Task<HookResult> OnDeletedAsync(ReturnRequest entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var returnRequests = entries
                .Select(x => x.Entity)
                .OfType<ReturnRequest>()
                .ToList();

            var orderItemIds = returnRequests.ToDistinctArray(x => x.OrderItemId);

            if (orderItemIds.Any())
            {
                var orders = await _db.OrderItems
                    .Include(x => x.Order)
                    .Where(x => orderItemIds.Contains(x.Id))
                    .Select(x => x.Order)
                    .ToListAsync(cancelToken);

                if (orders.Any())
                {
                    foreach (var groupedOrders in orders.GroupBy(x => x.Id))
                    {
                        await _eventPublisher.PublishOrderUpdatedAsync(groupedOrders.FirstOrDefault());
                    }
                }
            }
        }
    }
}
