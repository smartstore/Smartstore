using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Smartstore.Events;

public class EventPublisher : IEventPublisher
{
    private readonly IConsumerRegistry _registry;
    private readonly IConsumerResolver _resolver;
    private readonly IConsumerInvoker _invoker;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EventPublisher(
        IConsumerRegistry registry,
        IConsumerResolver resolver,
        IConsumerInvoker invoker,
        IHttpContextAccessor httpContextAccessor)
    {
        _registry = registry;
        _resolver = resolver;
        _invoker = invoker;
        _httpContextAccessor = httpContextAccessor;
    }

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public virtual void Publish<T>(T message) where T : IEventMessage
    {
        var descriptors = _registry.GetConsumers(message);

        if (descriptors.Length == 0)
        {
            return;
        }

        // Throw if any non-fire-and-forget consumer is async: invoking it synchronously would require
        // sync-over-async, which risks deadlocks and is explicitly not supported here.
        var asyncDescriptor = Array.Find(descriptors, static d => d.IsAsync && !d.FireForget);
        if (asyncDescriptor != null)
        {
            throw new InvalidOperationException(
                $"Cannot publish event '{typeof(T).Name}' synchronously because consumer '{asyncDescriptor.ContainerType.FullName}.{asyncDescriptor.Method.Name}' is asynchronous. Use PublishAsync() instead.");
        }

        var envelope = new ConsumeContext<T>(message);
        if (descriptors.Any(x => x.WithEnvelope))
        {
            envelope.Initialize(_httpContextAccessor.HttpContext);
        }

        for (var i = 0; i < descriptors.Length; i++)
        {
            var d = descriptors[i];
            var consumer = _resolver.Resolve(d);

            if (consumer != null)
            {
                // All remaining consumers are either sync or fire-and-forget.
                // InvokeAsync returns Task.CompletedTask for sync consumers, so Await() is safe.
                _invoker.InvokeAsync(d, consumer, envelope).Await();
            }
        }
    }

    public virtual async Task PublishAsync<T>(T message, CancellationToken cancelToken = default) where T : IEventMessage
    {
        var descriptors = _registry.GetConsumers(message);

        if (descriptors.Length == 0)
        {
            return;
        }

        var envelope = new ConsumeContext<T>(message);
        if (descriptors.Any(x => x.WithEnvelope))
        {
            envelope.Initialize(_httpContextAccessor.HttpContext);
        }

        for (var i = 0; i < descriptors.Length; i++)
        {
            var d = descriptors[i];
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