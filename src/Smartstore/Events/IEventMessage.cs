namespace Smartstore.Events
{
    /// <summary>
    /// Marker interface for event payload types used by the Smartstore event pipeline.
    /// Implement on message DTOs that are published via <see cref="IEventPublisher"/> and consumed by <see cref="IConsumer"/>.
    /// Contains no members by design and is used for type discovery, constraints, and dispatching.
    /// </summary>
    public interface IEventMessage
    {
    }
}
