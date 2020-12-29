using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Messages;

namespace Smartstore.Services.Messages
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
        Task<bool> SendMailsAsync(IEnumerable<QueuedEmail> queuedEmails, CancellationToken cancelToken);

        // TODO: (MH) (core) This is only used in one ocasion. Use code there (QueuedEmailController > DownloadAttachment) directly. 
        /// <summary>
        /// Loads binary data of a queued email attachment.
        /// </summary>
        /// <param name="attachment">Queued email attachment</param>
        /// <returns>Binary data if <c>attachment.StorageLocation</c> is <c>EmailAttachmentStorageLocation.Blob</c>, otherwise <c>null</c></returns>
        byte[] LoadQueuedEmailAttachmentBinary(QueuedEmailAttachment attachment);
    }
}
