namespace Smartstore.Core.Messaging.Events
{
    /// <summary>
    /// Published after the creation of a single message model part has been completed.
    /// </summary>
    /// <typeparam name="T">Type of source entity</typeparam>
    public class MessageModelPartCreatedEvent<T> where T : class
    {
        public MessageModelPartCreatedEvent(T source, dynamic part)
        {
            Source = source;
            Part = part;
        }

        /// <summary>
        /// The source object for which the model part has been created, e.g. a Product entity.
        /// </summary>
        public T Source { get; init; }

        /// <summary>
        /// The resulting model part.
        /// </summary>
        public dynamic Part { get; init; }
    }
}
