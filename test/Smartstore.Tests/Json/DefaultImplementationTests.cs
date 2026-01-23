#nullable enable

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NUnit.Framework;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Security;
using Smartstore.Json;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Json;

[TestFixture]
public class DefaultImplementationTests
{
    [Test]
    public void Resolve_returns_declared_type_for_concrete_types()
    {
        DefaultImplementationAttribute.Resolve(typeof(ConcreteType)).ShouldEqual(typeof(ConcreteType));
    }

    [Test]
    public void Resolve_returns_declared_type_if_attribute_missing()
    {
        DefaultImplementationAttribute.Resolve(typeof(INoDefault)).ShouldEqual(typeof(INoDefault));
    }

    [Test]
    public void Resolve_returns_default_implementation_for_interface()
    {
        DefaultImplementationAttribute.Resolve(typeof(IWithDefault)).ShouldEqual(typeof(DefaultImpl));
    }

    [Test]
    public void Resolve_returns_default_implementation_for_abstract_base_type()
    {
        DefaultImplementationAttribute.Resolve(typeof(AbstractWithDefault)).ShouldEqual(typeof(AbstractDefaultImpl));
    }

    [Test]
    public void Resolve_throws_if_implementation_is_not_concrete()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DefaultImplementationAttribute.Resolve(typeof(IWithAbstractDefault)));

        Assert.That(ex!.Message, Does.Contain("must be concrete"));
    }

    [Test]
    public void Resolve_throws_if_implementation_is_not_assignable()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DefaultImplementationAttribute.Resolve(typeof(IWithNonAssignableDefault)));

        Assert.That(ex!.Message, Does.Contain("is not assignable"));
    }

    [Test]
    public void Modifier_sets_CreateObject_for_abstract_declared_types_with_attribute()
    {
        var options = SmartJsonOptions.Default;

        var ti = options.GetTypeInfo(typeof(IWithDefault));

        ti.Kind.ShouldEqual(JsonTypeInfoKind.Object);
        ti.CreateObject.ShouldNotBeNull();

        var instance = ti.CreateObject!();
        instance.ShouldNotBeNull();
        Assert.That(instance, Is.TypeOf<DefaultImpl>());
    }

    [Test]
    public void Modifier_does_not_override_existing_CreateObject_factory()
    {
        var options = SmartJsonOptions.Default.Create(o =>
        {
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(typeInfo =>
            {
                if (typeInfo.Type == typeof(IWithDefault))
                {
                    typeInfo.CreateObject = () => new DefaultImpl { Value = "from-factory" };
                }
            });

            // Ensure DefaultImplementationModifier is in the chain after our custom modifier.
            o.TypeInfoResolver = resolver.WithDefaultImplementationModifier();
        });

        var obj = JsonSerializer.Deserialize<IWithDefault>("{\"Value\":\"from-json\"}", options);
        Assert.That(obj, Is.TypeOf<DefaultImpl>());
        obj!.Value.ShouldEqual("from-json");
    }

    [Test]
    public void JsonSerializer_deserializes_interface_declared_types_to_default_implementation()
    {
        var json = "{\"Value\":\"Hello\"}";

        var obj = JsonSerializer.Deserialize<IWithDefault>(json, SmartJsonOptions.Default);

        obj.ShouldNotBeNull();
        Assert.That(obj, Is.TypeOf<DefaultImpl>());
        obj!.Value.ShouldEqual("Hello");
    }

    [Test]
    public void Resolve_returns_default_implementation_for_ICategoryNode()
    {
        DefaultImplementationAttribute.Resolve(typeof(ICategoryNode)).ShouldEqual(typeof(CategoryNode));
    }

    [Test]
    public void JsonSerializer_deserializes_ICategoryNode_to_CategoryNode_without_type_discriminator()
    {
        var json = "{\"Id\":1,\"Name\":\"Cat\"}";

        var node = JsonSerializer.Deserialize<ICategoryNode>(json, SmartJsonOptions.Default);

        node.ShouldNotBeNull();
        Assert.That(node, Is.TypeOf<CategoryNode>());
        // ICategoryNode members are read-only; the main contract here is that we can materialize
        // a CategoryNode instance without a type discriminator.
    }

    [Test]
    public void JsonSerializer_can_roundtrip_TreeNode_of_interface_type_using_default_implementation()
    {
        var json = "{\"Value\":{\"Id\":1,\"Name\":\"Root\"},\"Children\":[{\"Value\":{\"Id\":2,\"Name\":\"Child\"}}]}";

        var tree = JsonSerializer.Deserialize<TreeNode<ICategoryNode>>(json, SmartJsonOptions.Default);

        tree.ShouldNotBeNull();
        Assert.That(tree!.Value, Is.TypeOf<CategoryNode>());
        tree.Children.Count.ShouldEqual(1);
        Assert.That(tree.Children[0].Value, Is.TypeOf<CategoryNode>());
    }

    [Test]
    public void Modifier_applies_implementation_conventions_to_interface_properties()
    {
        // CategoryNode decorates several properties ([JsonIgnore(WhenWritingDefault)]).
        // The modifier should copy the already materialized property behavior to the interface type,
        // so default values are omitted even when serializing an ICategoryNode reference.
        ICategoryNode model = new CategoryNode
        {
            Id = 1,
            Name = "Cat",
            Published = false,
            DisplayOrder = 0,
            BadgeStyle = 0,
            SubjectToAcl = false,
            LimitedToStores = false
        };

        var json = JsonSerializer.Serialize(model, SmartJsonOptions.Default);

        Assert.That(json, Does.Contain("\"Id\":1"));
        Assert.That(json, Does.Contain("\"Name\":\"Cat\""));

        // These properties should be omitted because they are default and CategoryNode marks them with JsonIgnore.WhenWritingDefault.
        Assert.That(json, Does.Not.Contain("Published"));
        Assert.That(json, Does.Not.Contain("DisplayOrder"));
        Assert.That(json, Does.Not.Contain("BadgeStyle"));
        Assert.That(json, Does.Not.Contain("SubjectToAcl"));
        Assert.That(json, Does.Not.Contain("LimitedToStores"));
    }

    [Test]
    public void JsonSerializer_deserializes_Multimap_with_IPermissionNode_values_to_concrete_nodes()
    {
        // Multimap JSON converter expects an array of { "Key": ..., "Value": [...] } items.
        var json = "[" +
            "{\"Key\":1,\"Value\":[{\"PermissionRecordId\":10,\"SystemName\":\"a\"}]}" +
            "," +
            "{\"Key\":2,\"Value\":[{\"PermissionRecordId\":20,\"SystemName\":\"b\",\"Allow\":true}]}" +
            "]";

        var map = JsonSerializer.Deserialize<Multimap<int, IPermissionNode>>(json, SmartJsonOptions.Default);

        map.ShouldNotBeNull();
        map!.Count.ShouldEqual(2);

        map[1].Count.ShouldEqual(1);
        Assert.That(map[1].Single(), Is.TypeOf<PermissionNode>());

        map[2].Count.ShouldEqual(1);
        Assert.That(map[2].Single(), Is.TypeOf<PermissionNode>());
    }

    [Test]
    public void Modifier_applies_implementation_conventions_for_nullable_property_in_IPermissionNode()
    {
        var map = new Multimap<int, IPermissionNode>();
        map.Add(1, new PermissionNode { PermissionRecordId = 10, SystemName = "a", Allow = null });

        var json = JsonSerializer.Serialize(map, SmartJsonOptions.Default);

        Assert.That(json, Does.Contain("\"PermissionRecordId\":10"));
        Assert.That(json, Does.Contain("\"SystemName\":\"a\""));
        Assert.That(json, Does.Not.Contain("\"Allow\""));
    }

    private sealed class ConcreteType { }

    private interface INoDefault { }

    [DefaultImplementation(typeof(DefaultImpl))]
    private interface IWithDefault
    {
        string? Value { get; set; }
    }

    private sealed class DefaultImpl : IWithDefault
    {
        public string? Value { get; set; }
    }

    [DefaultImplementation(typeof(AbstractDefaultImpl))]
    private abstract class AbstractWithDefault
    {
        public string? Value { get; set; }
    }

    private sealed class AbstractDefaultImpl : AbstractWithDefault { }

    [DefaultImplementation(typeof(AbstractImpl))]
    private interface IWithAbstractDefault { }

    private abstract class AbstractImpl : IWithAbstractDefault { }

    [DefaultImplementation(typeof(NotAssignable))]
    private interface IWithNonAssignableDefault { }

    private sealed class NotAssignable { }

}
