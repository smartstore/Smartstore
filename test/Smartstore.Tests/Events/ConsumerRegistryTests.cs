using System;
using NUnit.Framework;
using Smartstore.Events;

namespace Smartstore.Tests.Events;

#region Fake message types

public interface IOrderEvent : IEventMessage { }
public class OrderEventBase : IOrderEvent { }
public class OrderPlacedEvent : OrderEventBase { }
public class OrderShippedEvent : OrderEventBase { }
public class UnrelatedEvent : IEventMessage { }

#endregion

#region Fake consumers

// Registers for the exact concrete type
class ExactOrderPlacedConsumer : IConsumer
{
    public void Handle(OrderPlacedEvent msg) { }
}

// Registers for base class only
class BaseOrderConsumer : IConsumer
{
    public void Handle(OrderEventBase msg) { }
}

// Registers for interface only
class InterfaceOrderConsumer : IConsumer
{
    public void Handle(IOrderEvent msg) { }
}

// Registers for all three levels in one class
class AllLevelsConsumer : IConsumer
{
    public void Handle(OrderPlacedEvent msg) { }
    public void HandleEvent(OrderEventBase msg) { }
    public void Consume(IOrderEvent msg) { }
}

// WithEnvelope via ConsumeContext<T>
class EnvelopeConsumeContextConsumer : IConsumer
{
    public void Handle(ConsumeContext<OrderPlacedEvent> ctx) { }
}

// WithEnvelope via covariant IConsumeContext<T>
class EnvelopeIConsumeContextConsumer : IConsumer
{
    public void Handle(IConsumeContext<OrderEventBase> ctx) { }
}

// Two methods for the same underlying message type → should throw
class AmbiguousConsumer : IConsumer
{
    public void Handle(OrderPlacedEvent msg) { }
    public void HandleEvent(OrderPlacedEvent msg) { }
}

// Non-void, non-Task return type → should throw
class InvalidReturnTypeConsumer : IConsumer
{
    public int Handle(OrderPlacedEvent msg) => 0;
}

// Method with ref parameter → should throw
class RefParameterConsumer : IConsumer
{
    public void Handle(OrderPlacedEvent msg, ref int x) { }
}

// Async method named without "Async" suffix → should throw
class BadAsyncNameConsumer : IConsumer
{
    public void HandleAsync(OrderPlacedEvent msg) { }
}

#endregion

[TestFixture]
public class ConsumerRegistryTests
{
    // Creates a Lazy consumer entry; the factory is never called by ConsumerRegistry itself.
    private static Lazy<IConsumer, EventConsumerMetadata> Consumer<T>() where T : IConsumer
        => new Lazy<IConsumer, EventConsumerMetadata>(static () => null!, new EventConsumerMetadata { ContainerType = typeof(T) });

    private static ConsumerRegistry Registry(params Lazy<IConsumer, EventConsumerMetadata>[] consumers)
        => new ConsumerRegistry(consumers);

    // ----- exact type (fast path: type is in _expandedMap) -----

    [Test]
    public void ExactType_ReturnsSingleDescriptor()
    {
        var registry = Registry(Consumer<ExactOrderPlacedConsumer>());
        var descriptors = registry.GetConsumers(new OrderPlacedEvent());

        Assert.That(descriptors, Has.Length.EqualTo(1));
        Assert.That(descriptors[0].MessageType, Is.EqualTo(typeof(OrderPlacedEvent)));
        Assert.That(descriptors[0].WithEnvelope, Is.False);
    }

    // ----- base class only (lazy path: OrderPlacedEvent not in _expandedMap) -----

    [Test]
    public void DerivedType_ReturnsBaseConsumer_ViaLazyFallback()
    {
        var registry = Registry(Consumer<BaseOrderConsumer>());
        var descriptors = registry.GetConsumers(new OrderPlacedEvent());

        Assert.That(descriptors, Has.Length.EqualTo(1));
        Assert.That(descriptors[0].MessageType, Is.EqualTo(typeof(OrderEventBase)));
    }

    // ----- derived + base both registered (fast path for OrderPlacedEvent) -----

    [Test]
    public void DerivedAndBaseRegistered_ReturnsBothInOrder_ViaFastPath()
    {
        var registry = Registry(Consumer<ExactOrderPlacedConsumer>(), Consumer<BaseOrderConsumer>());
        var descriptors = registry.GetConsumers(new OrderPlacedEvent());

        Assert.That(descriptors, Has.Length.EqualTo(2));
        Assert.That(descriptors[0].MessageType, Is.EqualTo(typeof(OrderPlacedEvent)));
        Assert.That(descriptors[1].MessageType, Is.EqualTo(typeof(OrderEventBase)));
    }

    // ----- interface only (lazy path) -----

    [Test]
    public void DerivedType_ReturnsInterfaceConsumer_ViaLazyFallback()
    {
        var registry = Registry(Consumer<InterfaceOrderConsumer>());
        var descriptors = registry.GetConsumers(new OrderPlacedEvent());

        Assert.That(descriptors, Has.Length.EqualTo(1));
        Assert.That(descriptors[0].MessageType, Is.EqualTo(typeof(IOrderEvent)));
    }

    // ----- all three levels in a single consumer (fast path) -----

    [Test]
    public void AllLevels_ReturnsDescriptorsInHierarchyOrder()
    {
        var registry = Registry(Consumer<AllLevelsConsumer>());
        var descriptors = registry.GetConsumers(new OrderPlacedEvent());

        // concrete → base class → interface
        Assert.That(descriptors, Has.Length.EqualTo(3));
        Assert.That(descriptors[0].MessageType, Is.EqualTo(typeof(OrderPlacedEvent)));
        Assert.That(descriptors[1].MessageType, Is.EqualTo(typeof(OrderEventBase)));
        Assert.That(descriptors[2].MessageType, Is.EqualTo(typeof(IOrderEvent)));
    }

    // ----- sibling types resolved independently -----

    [Test]
    public void SiblingTypes_ResolveIndependently()
    {
        var registry = Registry(Consumer<BaseOrderConsumer>());

        var placed = registry.GetConsumers(new OrderPlacedEvent());
        var shipped = registry.GetConsumers(new OrderShippedEvent());

        Assert.That(placed, Has.Length.EqualTo(1));
        Assert.That(shipped, Has.Length.EqualTo(1));
        Assert.That(placed[0].MessageType, Is.EqualTo(typeof(OrderEventBase)));
        Assert.That(shipped[0].MessageType, Is.EqualTo(typeof(OrderEventBase)));
    }

    // ----- unrelated type → empty -----

    [Test]
    public void UnrelatedType_ReturnsEmpty()
    {
        var registry = Registry(Consumer<ExactOrderPlacedConsumer>());

        Assert.That(registry.GetConsumers(new UnrelatedEvent()), Is.Empty);
    }

    // ----- lazy cache: same array reference on repeated calls -----

    [Test]
    public void LazyCache_ReturnsSameArrayReferenceOnRepeatedCalls()
    {
        // BaseOrderConsumer only → OrderPlacedEvent goes through lazy path
        var registry = Registry(Consumer<BaseOrderConsumer>());

        var first = registry.GetConsumers(new OrderPlacedEvent());
        var second = registry.GetConsumers(new OrderPlacedEvent());

        Assert.That(first, Is.SameAs(second));
    }

    // ----- WithEnvelope: ConsumeContext<T> -----

    [Test]
    public void ConsumeContextEnvelope_SetsWithEnvelopeAndExtractsMessageType()
    {
        var registry = Registry(Consumer<EnvelopeConsumeContextConsumer>());
        var descriptors = registry.GetConsumers(new OrderPlacedEvent());

        Assert.That(descriptors, Has.Length.EqualTo(1));
        Assert.That(descriptors[0].WithEnvelope, Is.True);
        Assert.That(descriptors[0].MessageType, Is.EqualTo(typeof(OrderPlacedEvent)));
    }

    // ----- WithEnvelope: IConsumeContext<T> (covariant, base-type envelope) -----

    [Test]
    public void IConsumeContextEnvelope_SetsWithEnvelopeAndExtractsMessageType()
    {
        // Consumer declared for IConsumeContext<OrderEventBase>; publish OrderPlacedEvent
        // → covariant: ConsumeContext<OrderPlacedEvent> satisfies IConsumeContext<OrderEventBase>
        var registry = Registry(Consumer<EnvelopeIConsumeContextConsumer>());
        var descriptors = registry.GetConsumers(new OrderPlacedEvent());

        Assert.That(descriptors, Has.Length.EqualTo(1));
        Assert.That(descriptors[0].WithEnvelope, Is.True);
        Assert.That(descriptors[0].MessageType, Is.EqualTo(typeof(OrderEventBase)));
    }

    // ----- validation: ambiguous consumer (same message type, two methods) -----

    [Test]
    public void Construction_AmbiguousMethods_Throws()
    {
        Assert.Throws<AmbigousConsumerException>(() => Registry(Consumer<AmbiguousConsumer>()));
    }

    // ----- validation: non-void / non-Task return type -----

    [Test]
    public void Construction_InvalidReturnType_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => Registry(Consumer<InvalidReturnTypeConsumer>()));
    }

    // ----- validation: ref parameter -----

    [Test]
    public void Construction_RefParameter_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => Registry(Consumer<RefParameterConsumer>()));
    }

    // ----- validation: async method without "Async" suffix -----

    [Test]
    public void Construction_SyncMethodWitAsyncSuffix_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => Registry(Consumer<BadAsyncNameConsumer>()));
    }

    // ----- empty registry -----

    [Test]
    public void EmptyRegistry_ReturnsEmpty()
    {
        var registry = Registry();

        Assert.That(registry.GetConsumers(new OrderPlacedEvent()), Is.Empty);
    }
}