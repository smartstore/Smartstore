#nullable enable

namespace Smartstore.Core.Messaging;

public partial interface IQueuedEmailService
{
    /// <summary>
    /// Queues an email for delivery, optionally saving it immediately.
    /// </summary>
    /// <param name="queuedEmail">The new email to queue, including its attachments.</param>
    /// <param name="messageContext">
    /// The context used to create a template message. If supplied, publishes
    /// <see cref="Events.MessageQueuingEvent"/> before adding the email to the queue.
    /// Omit for emails created without a message template.
    /// </param>
    /// <param name="saveChanges">
    /// Whether to save changes in the current database context. If <c>false</c>, the caller
    /// must save changes later, for example after queuing a batch of campaign emails.
    /// </param>
    /// <param name="cancelToken">The cancellation token.</param>
    /// <remarks>
    /// With a message context, event consumers can alter the email and add attachments before
    /// persistence. Without a context, no message event is published. Saving includes all
    /// pending changes in the current database context and does not commit an enclosing transaction.
    /// </remarks>
    Task QueueEmailAsync(
        QueuedEmail queuedEmail,
        MessageContext? messageContext = null,
        bool saveChanges = true,
        CancellationToken cancelToken = default);

    /// <summary>
    /// Deletes all queued emails.
    /// </summary>
    /// <param name="olderThan">Delete only entries that are older than the given date.</param>
    /// <returns>The number of deleted entries.</returns>
    Task<int> DeleteAllQueuedMailsAsync(DateTime? olderThan = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Sends queued emails asynchronously. 
    /// </summary>
    /// <param name="queuedEmails">Queued emails. Entities must be tracked.</param>
    /// <returns>Whether the operation succeeded</returns>
    Task<bool> SendMailsAsync(IEnumerable<QueuedEmail> queuedEmails, CancellationToken cancelToken = default);
}