namespace Smartstore.Core.Messaging
{
    public partial interface IQueuedEmailService
    {
        /// <summary>
        /// Deletes all queued emails.
        /// </summary>
        /// <param name="olderThan">Delete only entries that are older than the given date.</param>
        /// <returns>The count of deleted entries.</returns>
        Task<int> DeleteAllQueuedMailsAsync(DateTime? olderThan = null, CancellationToken cancelToken = default);

        /// <summary>
        /// Sends queued emails asynchronously. 
        /// </summary>
        /// <param name="queuedEmails">Queued emails. Entities must be tracked.</param>
        /// <returns>Whether the operation succeeded</returns>
        Task<bool> SendMailsAsync(IEnumerable<QueuedEmail> queuedEmails, CancellationToken cancelToken = default);
    }
}
