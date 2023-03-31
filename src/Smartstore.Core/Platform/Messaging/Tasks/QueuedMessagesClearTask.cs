using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Data;
using Smartstore.Scheduling;
using Smartstore.Utilities;

namespace Smartstore.Core.Messaging.Tasks
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

            var numDeleted = await _db.QueuedEmails
                .Where(x => x.SentOnUtc.HasValue && x.CreatedOnUtc < olderThan)
                .ExecuteDeleteAsync(cancellationToken: cancelToken);

            if (numDeleted > 100 && _db.DataProvider.CanShrink)
            {
                await CommonHelper.TryAction(() => _db.DataProvider.ShrinkDatabaseAsync(true, cancelToken));
            }
        }
    }
}
