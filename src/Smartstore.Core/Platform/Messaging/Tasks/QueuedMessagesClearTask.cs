using Smartstore.Core.Common.Configuration;
using Smartstore.Scheduling;

namespace Smartstore.Core.Messaging.Tasks
{
    /// <summary>
    /// A task that periodically deletes sent emails from the message queue.
    /// </summary>
    public partial class QueuedMessagesClearTask : ITask
    {
        private readonly IQueuedEmailService _qeService;
        private readonly CommonSettings _commonSettings;

        public QueuedMessagesClearTask(IQueuedEmailService qeService, CommonSettings commonSettings)
        {
            _qeService = qeService;
            _commonSettings = commonSettings;
        }

        public Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            var olderThan = DateTime.UtcNow.AddDays(-Math.Abs(_commonSettings.MaxQueuedMessagesAgeInDays));
            return _qeService.DeleteAllQueuedMailsAsync(olderThan, cancelToken);
        }
    }
}
