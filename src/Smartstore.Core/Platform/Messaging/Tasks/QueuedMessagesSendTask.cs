using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore;
using Smartstore.Core.Data;
using Smartstore.Core.Scheduling;
using Smartstore.Core.Messages;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Services.Messages.Tasks
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
                    .Include("Attachments")
                    .ToPagedList(pageIndex, 1000)
                    .LoadAsync();

                await _queuedEmailService.SendMailsAsync(queuedEmails, cancelToken);

                if (!queuedEmails.HasNextPage || cancelToken.IsCancellationRequested)
                    break;

                pageIndex++;
            }
        }
    }
}
