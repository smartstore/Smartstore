#nullable enable annotations

using Smartstore.Events;

namespace Smartstore.Core.Messaging.Events;

/// <summary>
/// Associates an attachment placeholder with its generation instruction until persistence assigns its identifier.
/// </summary>
internal sealed record DeferredAttachmentRegistration(QueuedEmailAttachment Attachment, AttachmentWork Work);

/// <summary>
/// An event message which gets published just before a new instance of <see cref="QueuedEmail"/> is persisted to the database.
/// </summary>
public class MessageQueuingEvent : IEventMessage
{
    private readonly List<DeferredAttachmentRegistration> _deferredAttachments = [];

    public QueuedEmail QueuedEmail { get; init; }
    public MessageContext MessageContext { get; init; }
    public TemplateModel MessageModel { get; init; }

    /// <summary>
    /// Gets whether this queue operation accepts deferred attachments. Defaults to <c>false</c>.
    /// </summary>
    public bool AllowDeferredAttachments { get; init; }

    internal IReadOnlyList<DeferredAttachmentRegistration> DeferredAttachments => _deferredAttachments;

    /// <summary>
    /// Adds a new attachment placeholder and registers the work that will fill it later.
    /// </summary>
    /// <param name="attachment">A new, empty attachment containing its initial metadata and storage location.</param>
    /// <param name="work">The generation instruction. Registering it does not start background work.</param>
    /// <exception cref="InvalidOperationException">This queue operation does not accept deferred attachments.</exception>
    /// <exception cref="ArgumentException">The attachment is persisted, contains content, or has already been added.</exception>
    /// <remarks>
    /// The registration retains the attachment instance until persistence assigns its identifier.
    /// The queue infrastructure must replace this instance with its identifier before starting background work.
    /// </remarks>
    public void AddDeferredAttachment(QueuedEmailAttachment attachment, AttachmentWork work)
    {
        Guard.NotNull(attachment);
        Guard.NotNull(work);
        Guard.NotNull(QueuedEmail);

        if (!AllowDeferredAttachments)
        {
            throw new InvalidOperationException("This queue operation does not accept deferred attachments.");
        }

        if (attachment.Id != 0
            || attachment.MediaStorageId.HasValue
            || attachment.MediaStorage != null
            || attachment.MediaFileId.HasValue
            || attachment.MediaFile != null
            || attachment.Path.HasValue())
        {
            throw new ArgumentException("A new, empty attachment is required.", nameof(attachment));
        }

        if (_deferredAttachments.Any(x => ReferenceEquals(x.Attachment, attachment))
            || QueuedEmail.Attachments.Any(x => ReferenceEquals(x, attachment)))
        {
            throw new ArgumentException("The attachment has already been added.", nameof(attachment));
        }

        attachment.IsPending = true;
        QueuedEmail.Attachments.Add(attachment);
        _deferredAttachments.Add(new(attachment, work));
    }
}