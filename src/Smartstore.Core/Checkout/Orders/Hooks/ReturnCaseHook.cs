using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders;

internal class ReturnCaseHook : AsyncDbSaveHook<ReturnCase>
{
    private readonly SmartDbContext _db;
    private readonly IEventPublisher _eventPublisher;

    public ReturnCaseHook(SmartDbContext db, IEventPublisher eventPublisher)
    {
        _db = db;
        _eventPublisher = eventPublisher;
    }

    protected override Task<HookResult> OnInsertingAsync(ReturnCase entity, IHookedEntity entry, CancellationToken cancelToken)
    {
        if (entity.Kind == ReturnCaseKind.Return
            && entity.CompletedOn == null
            && IsFinalStatus(entity.ReturnCaseStatus))
        {
            entity.CompletedOn = DateTime.UtcNow;
        }

        return Task.FromResult(HookResult.Ok);
    }

    protected override Task<HookResult> OnUpdatingAsync(ReturnCase entity, IHookedEntity entry, CancellationToken cancelToken)
    {
        if (entity.Kind == ReturnCaseKind.Return
            && entity.CompletedOn == null
            && entry.Entry.TryGetModifiedProperty(nameof(entity.ReturnCaseStatusId), out var previousStatusId)
            && !IsFinalStatus((ReturnCaseStatus)(int)previousStatusId)
            && IsFinalStatus(entity.ReturnCaseStatus))
        {
            entity.CompletedOn = DateTime.UtcNow;
        }

        return Task.FromResult(HookResult.Ok);
    }

    protected override Task<HookResult> OnDeletedAsync(ReturnCase entity, IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Ok);

    public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        var returnCases = entries
            .Select(x => x.Entity)
            .OfType<ReturnCase>()
            .ToList();

        var orderItemIds = returnCases.ToDistinctArray(x => x.OrderItemId);
        if (orderItemIds.Length > 0)
        {
            var orders = await _db.OrderItems
                .Include(x => x.Order)
                .Where(x => orderItemIds.Contains(x.Id))
                .Select(x => x.Order)
                .ToListAsync(cancelToken);
            if (orders.Count > 0)
            {
                foreach (var groupedOrders in orders.GroupBy(x => x.Id))
                {
                    await _eventPublisher.PublishOrderUpdatedAsync(groupedOrders.FirstOrDefault());
                }
            }
        }
    }

    private static bool IsFinalStatus(ReturnCaseStatus status)
        => status is ReturnCaseStatus.ItemsRepaired
            or ReturnCaseStatus.ItemsRefunded
            or ReturnCaseStatus.RequestRejected
            or ReturnCaseStatus.Cancelled;
}