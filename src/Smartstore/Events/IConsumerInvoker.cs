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
            ConsumeContext<TMessage> envelope,
            CancellationToken cancelToken = default) where TMessage : class;
    }
}