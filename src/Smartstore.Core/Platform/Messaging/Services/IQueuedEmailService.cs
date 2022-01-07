namespace Smartstore.Core.Messaging
{
    public partial interface IQueuedEmailService
    {
        /// <summary>
        /// Deletes all queued emails.
        /// </summary>
        /// <returns>The count of deleted entries.</returns>
        Task<int> DeleteAllQueuedMailsAsync();

        /// <summary>
        /// Sends queued emails asynchronously. 
        /// </summary>
        /// <param name="queuedEmails">Queued emails. Entities must be tracked.</param>
        /// <returns>Whether the operation succeeded</returns>
        Task<bool> SendMailsAsync(IEnumerable<QueuedEmail> queuedEmails, CancellationToken cancelToken = default);
    }
}
