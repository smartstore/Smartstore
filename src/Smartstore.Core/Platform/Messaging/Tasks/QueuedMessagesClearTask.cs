using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Scheduling;
using Smartstore.Data.Batching;

namespace Smartstore.Services.Messages.Tasks
{
    /// <summary>
    /// A task that periodically deletes sent emails from the message queue.
    /// </summary>
    public partial class QueuedMessagesClearTask : ITask
    {
        private readonly SmartDbContext _db;
        private readonly CommonSettings _commonSettings;

        public QueuedMessagesClearTask(SmartDbContext db, CommonSettings commonSettings)
        {
            _db = db;
            _commonSettings = commonSettings;
        }

        public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            var olderThan = DateTime.UtcNow.AddDays(-Math.Abs(_commonSettings.MaxQueuedMessagesAgeInDays));
            
            await _db.QueuedEmails.Where(x => x.SentOnUtc.HasValue && x.CreatedOnUtc < olderThan).BatchDeleteAsync(cancellationToken: cancelToken);

            await _db.DataProvider.ShrinkDatabaseAsync(cancelToken);
        }
    }
}
