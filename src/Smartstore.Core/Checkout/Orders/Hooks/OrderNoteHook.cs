using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    public class OrderNoteHook : AsyncDbSaveHook<OrderNote>
    {
        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;

        public OrderNoteHook(SmartDbContext db, IEventPublisher eventPublisher)
        {
            _db = db;
            _eventPublisher = eventPublisher;
        }

        protected override Task<HookResult> OnDeletedAsync(OrderNote entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var orderNotes = entries
                .Select(x => x.Entity)
                .OfType<OrderNote>()
                .ToList();

            var orderIds = orderNotes
                .Select(x => x.OrderId)
                .Distinct()
                .ToArray();

            if (orderIds.Any())
            {
                var orders = await _db.Orders
                    .AsNoTracking()
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
