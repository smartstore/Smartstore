using Smartstore.Core.Data;
using Smartstore.Scheduling;

namespace Smartstore.Core.Messaging.Tasks;

/// <summary>
/// A task that periodically sends queued messages.
/// </summary>
public partial class QueuedMessagesSendTask : ITask
{
    private readonly SmartDbContext _db;
    private readonly IQueuedEmailService _queuedEmailService;
    private readonly IEmailRateLimiter _rateLimiter;

    public QueuedMessagesSendTask(
        SmartDbContext db,
        IQueuedEmailService queuedEmailService,
        IEmailRateLimiter rateLimiter)
    {
        _db = db;
        _queuedEmailService = queuedEmailService;
        _rateLimiter = rateLimiter;
    }

    public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
    {
        const int maxPageSize = 500;
        int pageIndex = 0;
        int totalProcessed = 0;

        while (true)
        {
            var allowedCount = _rateLimiter.GetAllowedSendCount(maxPageSize);
            if (allowedCount <= 0)
            {
                break;
            }

            var pageSize = Math.Min(maxPageSize, allowedCount);

            var queuedEmails = await _db.QueuedEmails
                .Where(x => x.SentTries < 3 && x.SendManually == false)
                .ApplyTimeFilter(null, null, true)
                .ApplySorting(true)
                .Include(x => x.Attachments)
                .ToPagedList(pageIndex, pageSize)
                .LoadAsync(cancelToken: cancelToken);

            if (queuedEmails.Count > 0)
            {
                await _queuedEmailService.SendMailsAsync(queuedEmails, cancelToken);
                totalProcessed += queuedEmails.Count;
            }

            if (!queuedEmails.HasNextPage || cancelToken.IsCancellationRequested)
                break;

            if (totalProcessed >= allowedCount)
                break;

            pageIndex++;
        }
    }
}