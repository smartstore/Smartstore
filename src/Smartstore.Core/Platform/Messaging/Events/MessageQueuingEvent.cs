namespace Smartstore.Core.Messaging.Events
{
    /// <summary>
    /// An event message which gets published just before a new instance of <see cref="QueuedEmail"/> is persisted to the database.
    /// </summary>
    public class MessageQueuingEvent
    {
        public QueuedEmail QueuedEmail { get; init; }
        public MessageContext MessageContext { get; init; }
        public TemplateModel MessageModel { get; init; }
    }
}
