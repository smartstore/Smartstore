namespace Smartstore.Events
{
    /// <summary>
    /// Responsible for invoking event message handler methods.
    /// </summary>
    public interface IConsumerInvoker
    {
        Task InvokeAsync<TMessage>(
            ConsumerDescriptor descriptor,
            IConsumer consumer,
            TMessage message,
            CancellationToken cancelToken = default) where TMessage : class;
    }
}