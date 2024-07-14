using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    internal class OrderNoteHook(SmartDbContext db, IEventPublisher eventPublisher) : AsyncDbSaveHook<OrderNote>
    {
        private readonly SmartDbContext _db = db;
        private readonly IEventPublisher _eventPublisher = eventPublisher;

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var orderNotes = entries
                .Select(x => x.Entity)
                .OfType<OrderNote>()
                .ToList();

            var orderIds = orderNotes.ToDistinctArray(x => x.OrderId);

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
