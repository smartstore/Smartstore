using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.ComponentModel;

namespace Smartstore.Events
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IConsumerRegistry _registry;
        private readonly IConsumerResolver _resolver;
        private readonly IConsumerInvoker _invoker;

        public EventPublisher(IConsumerRegistry registry, IConsumerResolver resolver, IConsumerInvoker invoker)
        {
            _registry = registry;
            _resolver = resolver;
            _invoker = invoker;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual async Task PublishAsync<T>(T message, CancellationToken cancelToken = default) where T : class
        {
            var descriptors = _registry.GetConsumers(message);

            if (!descriptors.Any())
            {
                return;
            }

            var envelopeType = typeof(ConsumeContext<>).MakeGenericType(typeof(T));
            var envelope = (ConsumeContext<T>)FastActivator.CreateInstance(envelopeType, message);

            foreach (var d in descriptors)
            {
                var consumer = _resolver.Resolve(d);
                if (consumer != null)
                {
                    if (d.FireForget)
                    {
                        // No await
                        // "_ =" to discard 'async/await' compiler warning
                        _ = _invoker.InvokeAsync(d, consumer, envelope, cancelToken);
                    }
                    else
                    {
                        // Await the task
                        await _invoker.InvokeAsync(d, consumer, envelope, cancelToken);
                    }
                }
            }
        }
    }
}