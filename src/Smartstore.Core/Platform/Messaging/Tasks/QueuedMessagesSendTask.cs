using Smartstore.Core.Data;
using Smartstore.Scheduling;

namespace Smartstore.Core.Messaging.Tasks
{
    /// <summary>
    /// A task that periodically send queued messages.
    /// </summary>
    public partial class QueuedMessagesSendTask : ITask
    {
        private readonly SmartDbContext _db;
        private readonly IQueuedEmailService _queuedEmailService;

        public QueuedMessagesSendTask(SmartDbContext db, IQueuedEmailService queuedEmailService)
        {
            _db = db;
            _queuedEmailService = queuedEmailService;
        }

        public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            int pageIndex = 0;
            while (true)
            {
                var queuedEmails = await _db.QueuedEmails
                    .Where(x => x.SentTries < 3 && x.SendManually == false)
                    .ApplyTimeFilter(null, null, true)
                    .ApplySorting(true)
                    .Include(x => x.Attachments)
                    .ToPagedList(pageIndex, 500)
                    .LoadAsync(cancelToken: cancelToken);

                await _queuedEmailService.SendMailsAsync(queuedEmails, cancelToken);

                if (!queuedEmails.HasNextPage || cancelToken.IsCancellationRequested)
                    break;

                pageIndex++;
            }
        }
    }
}
