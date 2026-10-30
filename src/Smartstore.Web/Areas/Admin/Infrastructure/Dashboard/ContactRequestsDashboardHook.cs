using Smartstore.Core.Common.Services;
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
    private readonly IGenericAttributeService _attributes;
    private readonly SmartDbContext _db;

    internal const string AttributeKey = "Dashboard.ContactRequest";
    internal const int MaxEntries = 5;

    public ContactRequestsDashboardHook(IGenericAttributeService attributes, SmartDbContext db)
    {
        _attributes = attributes;
        _db = db;
    }

    /// <summary>
    /// Marks contact messages without requiring an ID before persistence.
    /// </summary>
    public void HandleEvent(MessageQueuingEvent message)
    {
        var context = message.MessageContext;
        string template = context?.MessageTemplate?.Name.NullEmpty() ?? context?.MessageTemplateName;
        if (context == null || context.TestMode || !MessageTemplateNames.SystemContactUs.EqualsNoCase(template))
            return;

        int storeId = context.Store?.Id ?? context.StoreId.GetValueOrDefault();
        string email = message.MessageModel?.Evaluate("Message.SenderEmail") as string;
        if (storeId > 0 && email.HasValue())
            message.QueuedEmail.AddHookState(AttributeKey, (StoreId: storeId, Email: email));
    }

    /// <inheritdoc />
    protected override async Task<HookResult> OnInsertedAsync(QueuedEmail entity, IHookedEntity entry, CancellationToken cancelToken)
    {
        if (entity.Id <= 0 || entity.GetHookState(AttributeKey) is not ValueTuple<int, string> contact)
            return HookResult.Ok;

        (int storeId, string email) = contact;
        var attributes = _attributes.GetAttributesForEntity(nameof(QueuedEmail), entity.Id);
        attributes.Set(AttributeKey, email, storeId);
        await attributes.SaveChangesAsync(cancelToken);
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
