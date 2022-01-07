using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Payment.Hooks
{
    internal class RecurringPaymentHook : AsyncDbSaveHook<RecurringPayment>
    {
        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;

        public RecurringPaymentHook(SmartDbContext db, IEventPublisher eventPublisher)
        {
            _db = db;
            _eventPublisher = eventPublisher;
        }

        protected override Task<HookResult> OnInsertedAsync(RecurringPayment entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnUpdatedAsync(RecurringPayment entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var recurringPayments = entries
                .Select(x => x.Entity)
                .OfType<RecurringPayment>()
                .ToList();

            var orderIds = recurringPayments.ToDistinctArray(x => x.InitialOrderId);

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
