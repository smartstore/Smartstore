namespace Smartstore.Core.Messaging.Events
{
    /// <summary>
    /// Published after the message model has been completely created.
    /// </summary>
    public class MessageModelCreatedEvent
    {
        public MessageModelCreatedEvent(MessageContext messageContext, TemplateModel model)
        {
            MessageContext = messageContext;
            Model = model;
        }

        public MessageContext MessageContext { get; init; }

        /// <summary>
        /// The resulting message model.
        /// </summary>
        public TemplateModel Model { get; init; }
    }

}
