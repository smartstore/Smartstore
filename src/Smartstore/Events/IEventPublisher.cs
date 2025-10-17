namespace Smartstore.Events
{
    /// <summary>
    /// Responsible for dispatching event messages to subscribers
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes an event messages.
        /// </summary>
        /// <param name="message">The message instance. Can be of any type.</param>
        Task PublishAsync<T>(T message, CancellationToken cancelToken = default) where T : IEventMessage;
    }

    public sealed class NullEventPublisher : IEventPublisher
    {
        public static NullEventPublisher Instance { get; } = new NullEventPublisher();

        public Task PublishAsync<T>(T message, CancellationToken cancelToken = default) where T : IEventMessage
        {
            return Task.CompletedTask;
        }
    }

    public static class IEventPublisherExtensions
    {
        /// <summary>
        /// Publishes an event messages.
        /// NOTE: Avoid calling this method, call the Async counterpart instead.
        /// </summary>
        /// <param name="message">The message instance. Can be of any type.</param>
        public static void Publish<T>(this IEventPublisher publisher, T message) where T : IEventMessage
        {
            publisher.PublishAsync(message).Await();
        }
    }
}