using Smartstore.Core.Common;
using Smartstore.Core.Messaging;
using Smartstore.Core.Messaging.Events;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Recognizes contact messages at creation and marks them after successful insertion.
/// </summary>
[Important]
internal sealed class ContactRequestsDashboardHook : AsyncDbSaveHook<QueuedEmail>, IConsumer
{
    private readonly SmartDbContext _db;

    internal const string AttributeKey = "Dashboard.ContactRequest";

    public ContactRequestsDashboardHook(SmartDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Marks contact messages without requiring an ID before persistence.
    /// </summary>
    public void HandleEvent(MessageQueuingEvent message)
    {
        var context = message.MessageContext;
        var template = context?.MessageTemplate?.Name.NullEmpty() ?? context?.MessageTemplateName;

        if (context == null || context.TestMode || !MessageTemplateNames.SystemContactUs.EqualsNoCase(template))
            return;

        var storeId = context.Store?.Id ?? context.StoreId.GetValueOrDefault();
        var email = message.MessageModel?.Evaluate("Message.SenderEmail") as string;

        if (storeId > 0 && email.HasValue())
        {
            message.QueuedEmail.AddHookState(AttributeKey, (StoreId: storeId, Email: email));
        }
    }

    /// <inheritdoc />
    protected override async Task<HookResult> OnInsertedAsync(QueuedEmail entity, IHookedEntity entry, CancellationToken cancelToken)
    {
        if (entity.Id <= 0 || entity.GetHookState(AttributeKey) is not ValueTuple<int, string> contact)
            return HookResult.Ok;

        (int storeId, string email) = contact;
        _db.GenericAttributes.Add(new GenericAttribute
        {
            KeyGroup = nameof(QueuedEmail),
            EntityId = entity.Id,
            Key = AttributeKey,
            StoreId = storeId,
            Value = email
        });
        await _db.SaveChangesAsync(cancelToken);
        return HookResult.Ok;
    }

    /// <inheritdoc />
    protected override Task<HookResult> OnDeletedAsync(QueuedEmail entity, IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Ok);

    /// <inheritdoc />
    public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        var deletedIds = entries
            .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
            .Select(x => x.Entity.Id)
            .ToArray();

        foreach (var chunk in deletedIds.Chunk(128))
        {
            await _db.GenericAttributes
                .Where(x => x.KeyGroup == nameof(QueuedEmail) && x.Key == AttributeKey && chunk.Contains(x.EntityId))
                .ExecuteDeleteAsync(cancelToken);
        }
    }
}
