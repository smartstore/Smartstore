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
    public class OrderItemHook : AsyncDbSaveHook<OrderItem>
    {
        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;

        public OrderItemHook(SmartDbContext db, IEventPublisher eventPublisher)
        {
            _db = db;
            _eventPublisher = eventPublisher;
        }

        protected override Task<HookResult> OnDeletedAsync(OrderItem entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var orderItems = entries
                .Select(x => x.Entity)
                .OfType<OrderItem>()
                .ToList();

            var orderIds = orderItems
                .Select(x => x.OrderId)
                .Distinct()
                .ToArray();

            if (orderIds.Any())
            {
                //var orders = await _db.Orders.GetManyAsync(orderIds);
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
