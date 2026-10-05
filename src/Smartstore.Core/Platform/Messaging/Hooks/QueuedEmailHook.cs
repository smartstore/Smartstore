using Autofac;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging.Events;
using Smartstore.Data.Hooks;
using Smartstore.Threading;

namespace Smartstore.Core.Messaging.Hooks;

[Important]
internal sealed class QueuedEmailHook : AsyncDbSaveHook<QueuedEmail>
{
    private readonly AsyncRunner _asyncRunner;

    public QueuedEmailHook(AsyncRunner asyncRunner)
    {
        _asyncRunner = asyncRunner;
    }

    protected override Task<HookResult> OnInsertedAsync(QueuedEmail entity, IHookedEntity entry, CancellationToken cancelToken)
    {
        if (entity.GetHookState(nameof(MessageQueuingEvent.DeferredAttachments)) is DeferredAttachmentRegistration[] registrations
            && registrations.Length > 0)
        {
            // Freeze the generated IDs before the hook state is cleared. No request entities enter the background task.
            var workItems = registrations
                .Select(x => (AttachmentId: x.Attachment.Id, x.Work))
                .ToArray();

            _ = _asyncRunner.RunTask(
                static (scope, token, state) => GenerateAttachmentsAsync(scope, ((int AttachmentId, AttachmentWork Work)[])state, token),
                workItems);
        }

        return Task.FromResult(HookResult.Ok);
    }

    private static async Task GenerateAttachmentsAsync(
        ILifetimeScope scope,
        (int AttachmentId, AttachmentWork Work)[] workItems,
        CancellationToken cancelToken)
    {
        var db = scope.Resolve<SmartDbContext>();
        var logger = scope.Resolve<ILogger<QueuedEmailHook>>();

        foreach (var item in workItems)
        {
            cancelToken.ThrowIfCancellationRequested();

            // Keep generation detached so failed work cannot persist partial attachment content.
            var attachment = await db.QueuedEmailAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == item.AttachmentId && x.IsPending, cancelToken);

            if (attachment == null)
            {
                continue;
            }

            using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            budget.CancelAfter(item.Work.Timeout);

            try
            {
                await item.Work.GenerateAsync(scope, attachment, budget.Token);
                budget.Token.ThrowIfCancellationRequested();
            }
            catch (Exception ex) when (!cancelToken.IsCancellationRequested)
            {
                logger.Error(ex, $"Failed to generate queued email attachment {item.AttachmentId}.");

                // Delete the persisted empty placeholder, without attaching the failed generation result.
                db.QueuedEmailAttachments.Remove(new QueuedEmailAttachment { Id = item.AttachmentId });
                await db.SaveChangesAsync(cancelToken);
                continue;
            }

            attachment.IsPending = false;
            db.Attach(attachment).State = EfState.Modified;
            await db.SaveChangesAsync(cancelToken);
        }
    }
}