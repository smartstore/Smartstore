namespace Smartstore.Events;

/// <summary>
/// Responsible for dispatching event messages to subscribers
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event message synchronously.
    /// Throws <see cref="InvalidOperationException"/> if any registered consumer for <typeparamref name="T"/> is asynchronous
    /// (i.e. has an async, non-fire-and-forget handler), because invoking an async consumer from a sync context
    /// would require sync-over-async, which is not supported.
    /// </summary>
    /// <param name="message">The message instance.</param>
    /// <exception cref="InvalidOperationException">Thrown when one or more registered consumers are asynchronous.</exception>
    void Publish<T>(T message) where T : IEventMessage;

    /// <summary>
    /// Publishes an event message asynchronously.
    /// </summary>
    /// <param name="message">The message instance.</param>
    Task PublishAsync<T>(T message, CancellationToken cancelToken = default) where T : IEventMessage;
}

public sealed class NullEventPublisher : IEventPublisher
{
    public static NullEventPublisher Instance { get; } = new NullEventPublisher();

    public void Publish<T>(T message) where T : IEventMessage 
    {
        // Noop
    }

    public Task PublishAsync<T>(T message, CancellationToken cancelToken = default) where T : IEventMessage
    {
        return Task.CompletedTask;
    }
}

