#nullable enable

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Smartstore.Events;

public class ConsumerRegistry : IConsumerRegistry
{
    private static readonly Type _openConsumeContextType = typeof(ConsumeContext<>);
    private static readonly Type _openIConsumeContextType = typeof(IConsumeContext<>);

    // Raw descriptor map: keyed by declared consumer message type.
    // Kept as a field because ResolveHierarchy reads it at runtime for lazy cache misses.
    private FrozenDictionary<Type, ConsumerDescriptor[]> _descriptorMap =
        FrozenDictionary<Type, ConsumerDescriptor[]>.Empty;

    // Pre-expanded at startup for every registered consumer type (and their resolved hierarchies).
    // FrozenDictionary gives the fastest possible read characteristics on the hot path.
    private FrozenDictionary<Type, ConsumerDescriptor[]> _expandedMap =
        FrozenDictionary<Type, ConsumerDescriptor[]>.Empty;

    // Lazy fallback for published types that have no direct consumer registration
    // (e.g. OrderPlacedEvent itself has no consumer, but OrderEventBase does).
    // ConcurrentDictionary: effectively lock-free after initial population per type.
    private readonly ConcurrentDictionary<Type, ConsumerDescriptor[]> _lazyCache = new();

    public ConsumerRegistry(IEnumerable<Lazy<IConsumer, EventConsumerMetadata>> consumers)
    {
        var tempMap = new Dictionary<Type, List<ConsumerDescriptor>>();

        foreach (var consumer in consumers)
        {
            var metadata = consumer.Metadata;
            var methods = FindMethods(metadata);
            var messageTypes = new Dictionary<Type, MethodInfo>();

            foreach (var method in methods)
            {
                var handleErrorAttr = method.GetCustomAttribute<HandleErrorAttribute>(false);

                var descriptor = new ConsumerDescriptor(metadata)
                {
                    IsAsync = method.ReturnType == typeof(Task),
                    FireForget = method.HasAttribute<FireForgetAttribute>(false),
                    LogError = handleErrorAttr?.Log ?? true,
                    ThrowError = handleErrorAttr?.Throw ?? true
                };

                //if (descriptor.IsAsync && descriptor.FireForget)
                //{
                //	throw new NotSupportedException($"An asynchronous message consumer method cannot be called as fire & forget. Method: '{method}'.");
                //}

                if (method.ReturnType != typeof(Task) && method.ReturnType != typeof(void))
                {
                    throw new NotSupportedException($"A message consumer method's return type must either be 'void' or '${typeof(Task).FullName}'. Method: '{method}'.");
                }

                if (method.ReturnType == typeof(void) && HasAsyncKeyword(method))
                {
                    throw new NotSupportedException($"The return type of an asynchronous message consumer method must not be 'void'. Method: '{method}'.");
                }

                if (method.Name.EndsWith("Async") && !descriptor.IsAsync)
                {
                    throw new NotSupportedException($"A synchronous message consumer method name should not end with 'Async'. Method: '{method}'.");
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    throw new NotSupportedException($"A message consumer method must have at least one parameter identifying the message to consume. Method: '{method}'.");
                }

                if (parameters.Any(x => x.ParameterType.IsByRef || x.IsOut || x.IsOptional))
                {
                    throw new NotSupportedException($"'out', 'ref' and optional parameters are not allowed in consumer methods. Method: '{method}'.");
                }

                var p = parameters[0];
                var messageType = p.ParameterType;

                // Detect ConsumeContext<T> and IConsumeContext<T> envelope types.
                // IConsumeContext<out T> enables covariant dispatch: a consumer declared for
                // IConsumeContext<OrderEventBase> receives any ConsumeContext<TDerived> via CLR covariance.
                if (messageType.IsGenericType)
                {
                    var openGeneric = messageType.GetGenericTypeDefinition();
                    if (openGeneric == _openConsumeContextType || openGeneric == _openIConsumeContextType)
                    {
                        messageType = messageType.GetGenericArguments()[0];
                        descriptor.WithEnvelope = true;
                    }
                }

                if (messageTypes.TryGetValue(messageType, out var method2))
                {
                    // We won't allow methods with different signatures, but same message type: there can only be one!
                    throw new AmbigousConsumerException(method2, method);
                }

                messageTypes.Add(messageType, method);

                if (messageType.IsPublic && (messageType.IsClass || messageType.IsInterface))
                {
                    // The method signature is valid: add to dictionary.
                    descriptor.MessageParameter = p;
                    descriptor.Parameters = parameters.Skip(1).ToArray();
                    descriptor.MessageType = messageType;
                    descriptor.Method = method;

                    if (!tempMap.TryGetValue(messageType, out var bucket))
                        tempMap[messageType] = bucket = [];
                    bucket.Add(descriptor);
                }
                else
                {
                    throw new NotSupportedException("Message types must be public classes.");
                }
            }
        }

        _descriptorMap = tempMap.ToFrozenDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value.ToArray());

        BuildExpandedMap();
    }

    /// <summary>
    /// Pre-computes the resolved descriptor arrays for all registered consumer types.
    /// Each entry already includes descriptors from base classes and interfaces,
    /// ordered from most specific (exact type) to most general (object, then interfaces).
    /// This eliminates any hierarchy traversal overhead on the hot path for known types.
    /// </summary>
    private void BuildExpandedMap()
    {
        if (_descriptorMap.Count == 0)
        {
            return;
        }

        var result = new Dictionary<Type, ConsumerDescriptor[]>(_descriptorMap.Count);

        foreach (var registeredType in _descriptorMap.Keys)
        {
            // ResolveHierarchy always returns a non-empty array here because
            // the registered type itself is in _descriptorMap.
            result[registeredType] = ResolveHierarchy(registeredType);
        }

        _expandedMap = result.ToFrozenDictionary();
    }

    /// <summary>
    /// Walks the type hierarchy of <paramref name="messageType"/> and collects all matching
    /// <see cref="ConsumerDescriptor"/> entries from <see cref="_descriptorMap"/>.
    /// Order: concrete type → base classes → object → implemented interfaces.
    /// </summary>
    private ConsumerDescriptor[] ResolveHierarchy(Type messageType)
    {
        List<ConsumerDescriptor>? list = null;

        // Walk class hierarchy: concrete → base classes → object
        var t = messageType;
        while (t != null)
        {
            if (_descriptorMap.TryGetValue(t, out var descs))
            {
                (list ??= []).AddRange(descs);
            }

            t = t.BaseType;
        }

        // Walk implemented interfaces (after full class chain)
        foreach (var iface in messageType.GetInterfaces())
        {
            if (_descriptorMap.TryGetValue(iface, out var descs))
            {
                (list ??= []).AddRange(descs);
            }
        }

        return list?.ToArray() ?? [];
    }

    private static IEnumerable<MethodInfo> FindMethods(EventConsumerMetadata metadata)
    {
        var methods = metadata.ContainerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        var validNames = new HashSet<string>(["Handle", "HandleEvent", "Consume", "HandleAsync", "HandleEventAsync", "ConsumeAsync"]);

        foreach (var method in methods)
        {
            if (validNames.Contains(method.Name))
            {
                yield return method;
            }
        }
    }

    private static bool HasAsyncKeyword(MethodInfo method)
    {
        return method.GetCustomAttribute<AsyncStateMachineAttribute>()
            ?.StateMachineType
            ?.GetTypeInfo()
            ?.GetCustomAttribute<CompilerGeneratedAttribute>() != null;
    }

    public virtual ConsumerDescriptor[] GetConsumers(object message)
    {
        Guard.NotNull(message);

        var type = message.GetType();

        // Fast path: FrozenDictionary — O(1), zero allocation, no locking.
        // Covers all types that have at least one direct consumer registration.
        if (_expandedMap.TryGetValue(type, out var descriptors))
        {
            return descriptors;
        }

        // Lazy fallback: type was published but has no direct consumer registration.
        // Resolves and caches the hierarchy exactly once per concrete type.
        // GetOrAdd with a factory argument avoids closure allocation on every call.
        var lazy = _lazyCache.GetOrAdd(type, static (t, self) => self.ResolveHierarchy(t), this);
        return lazy.Length > 0 ? lazy : [];
    }
}