using Smartstore.Core.Messages;

namespace Smartstore.Core.Messages.Events
{
    /// <summary>
    /// An event message which gets published just before a new instance of <see cref="QueuedEmail"/> is persisted to the database.
    /// </summary>
    public class MessageQueuingEvent
    {
        public QueuedEmail QueuedEmail { get; set; }
        public MessageContext MessageContext { get; set; }
        public TemplateModel MessageModel { get; set; }
    }
}
