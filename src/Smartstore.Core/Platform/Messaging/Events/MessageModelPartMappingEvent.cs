namespace Smartstore.Core.Messaging.Events
{
    /// <summary>
    /// Published when a system mapper is missing for a particular model type (e.g. a custom entity in a module).
    /// Implementors should subscribe to this event in order to provide a corresponding dynamic model part.
    /// The result model should be assigned to the <see cref="Result"/> property. If this property
    /// is still <c>null</c>, the source is used as model part instead.
    /// </summary>
    public class MessageModelPartMappingEvent
    {
        public MessageModelPartMappingEvent(object source, MessageContext messageContext)
        {
            Source = source;
            MessageContext = messageContext;
        }

        /// <summary>
        /// The source object for which a model part should be created.
        /// </summary>
        public object Source { get; init; }

        /// <summary>
        /// The message context.
        /// </summary>
        public MessageContext MessageContext { get; init; }

        /// <summary>
        /// The resulting model part.
        /// </summary>
        public dynamic Result { get; set; }

        /// <summary>
        /// The name of the model part. If <c>null</c> the source's type name is used.
        /// </summary>
        public string ModelPartName { get; set; }
    }
}
