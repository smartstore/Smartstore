using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using Smartstore.Collections;
using Smartstore.Collections.JsonConverters;
using Smartstore.Json;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Collections;

[TestFixture]
public class TreeNodeJsonConverterTests
{
    private TreeNodeJsonConverterFactory _converterFactory;
    private JsonSerializerOptions _options;

    [SetUp]
    public void Setup()
    {
        _converterFactory = new TreeNodeJsonConverterFactory();
        _options = SmartJsonOptions.Default.Create(o =>
        {
            o.Converters.Add(_converterFactory);
        });
    }

    [Test]
    public void Can_deserialize_simple_treenode()
    {
        var json = """{"Value":"Test"}""";

        var node = JsonSerializer.Deserialize<TreeNode<string>>(json, _options);

        node.ShouldNotBeNull();
        node.Value.ShouldEqual("Test");
        node.HasChildren.ShouldEqual(false);
        node.Id.ShouldBeNull();
        node.Metadata.ShouldNotBeNull();
        node.Metadata.Count.ShouldEqual(0);
    }

    [Test]
    public void Can_deserialize_treenode_with_children()
    {
        var json = """
        {
            "Value":"Parent",
            "Children":[
                {"Value":"Child1"},
                {"Value":"Child2"}
            ]
        }
        """;

        var node = JsonSerializer.Deserialize<TreeNode<string>>(json, _options);

        node.ShouldNotBeNull();
        node.Value.ShouldEqual("Parent");
        node.HasChildren.ShouldEqual(true);
        node.Children.Count.ShouldEqual(2);
        node.Children[0].Value.ShouldEqual("Child1");
        node.Children[1].Value.ShouldEqual("Child2");
    }

    [Test]
    public void Can_deserialize_treenode_with_single_id()
    {
        var json = """{"Value":"Test","Id":"node-1"}""";

        var node = JsonSerializer.Deserialize<TreeNode<string>>(json, _options);

        node.ShouldNotBeNull();
        node.Value.ShouldEqual("Test");
        node.Id.ShouldNotBeNull();
        node.Id.ToString().ShouldEqual("node-1");
    }

    [Test]
    public void Can_deserialize_treenode_with_id_array()
    {
        var json = """{"Value":"Test","Id":[1,2,3]}""";

        var node = JsonSerializer.Deserialize<TreeNode<string>>(json, _options);

        node.ShouldNotBeNull();
        node.Value.ShouldEqual("Test");
        node.Id.ShouldNotBeNull();
        
        var idArray = node.Id as object[];
        idArray.ShouldNotBeNull();
        idArray.Length.ShouldEqual(3);
        idArray[0].ShouldEqual(1);
        idArray[1].ShouldEqual(2);
        idArray[2].ShouldEqual(3);
    }

    [Test]
    public void Can_deserialize_treenode_with_metadata()
    {
        var json = """
        {
            "Value":"Test",
            "Metadata":{
                "key1":"value1",
                "key2":"value2"
            }
        }
        """;

        var node = JsonSerializer.Deserialize<TreeNode<string>>(json, _options);

        node.ShouldNotBeNull();
        node.Value.ShouldEqual("Test");
        node.Metadata.ShouldNotBeNull();
        node.Metadata.Count.ShouldEqual(2);
        node.Metadata["key1"].ToString().ShouldEqual("value1");
        node.Metadata["key2"].ToString().ShouldEqual("value2");
    }

    [Test]
    public void Can_deserialize_treenode_with_all_properties()
    {
        var json = """
        {
            "Id":"root",
            "Value":"Root Node",
            "Metadata":{
                "category":"test",
                "priority":"high"
            },
            "Children":[
                {
                    "Id":"child1",
                    "Value":"Child 1"
                },
                {
                    "Id":"child2",
                    "Value":"Child 2"
                }
            ]
        }
        """;

        var node = JsonSerializer.Deserialize<TreeNode<string>>(json, _options);

        node.ShouldNotBeNull();
        node.Id.ToString().ShouldEqual("root");
        node.Value.ShouldEqual("Root Node");
        node.Metadata.Count.ShouldEqual(2);
        node.Metadata["category"].ToString().ShouldEqual("test");
        node.Metadata["priority"].ToString().ShouldEqual("high");
        node.HasChildren.ShouldEqual(true);
        node.Children.Count.ShouldEqual(2);
        node.Children[0].Id.ToString().ShouldEqual("child1");
        node.Children[0].Value.ShouldEqual("Child 1");
        node.Children[1].Id.ToString().ShouldEqual("child2");
        node.Children[1].Value.ShouldEqual("Child 2");
    }

    [Test]
    public void Deserialize_throws_on_invalid_json()
    {
        var json = """["invalid"]""";

        Assert.Throws<JsonException>(() =>
        {
            JsonSerializer.Deserialize<TreeNode<string>>(json, _options);
        });
    }

    [Test]
    public void Deserialize_throws_on_missing_start_object()
    {
        var json = @"""Value"":""Test""";

        Assert.Throws<JsonException>(() =>
        {
            JsonSerializer.Deserialize<TreeNode<string>>(json, _options);
        });
    }

    [Test]
    public void Can_serialize_simple_treenode()
    {
        var node = new TreeNode<string>("Test");

        var json = JsonSerializer.Serialize(node, _options);

        json.ShouldNotBeNull();
        Assert.That(json, Does.Contain(@"""Value"":""Test"""));
        Assert.That(json, Does.Not.Contain("Id"));
        Assert.That(json, Does.Not.Contain("Metadata"));
        Assert.That(json, Does.Not.Contain("Children"));
    }

    [Test]
    public void Can_serialize_treenode_with_id()
    {
        var node = new TreeNode<string>("Test")
        {
            Id = "node-1"
        };

        var json = JsonSerializer.Serialize(node, _options);

        json.ShouldNotBeNull();
        Assert.That(json, Does.Contain(@"""Id"":""node-1"""));
        Assert.That(json, Does.Contain(@"""Value"":""Test"""));
    }

    [Test]
    public void Can_serialize_treenode_with_metadata()
    {
        var node = new TreeNode<string>("Test");
        node.Metadata["key1"] = "value1";
        node.Metadata["key2"] = 123;

        var json = JsonSerializer.Serialize(node, _options);

        json.ShouldNotBeNull();
        Assert.That(json, Does.Contain(@"""Value"":""Test"""));
        Assert.That(json, Does.Contain("Metadata"));
        Assert.That(json, Does.Contain(@"""key1"":""value1"""));
    }

    [Test]
    public void Can_serialize_treenode_with_children()
    {
        var child1 = new TreeNode<string>("Child1");
        var child2 = new TreeNode<string>("Child2");
        var parent = new TreeNode<string>("Parent", new List<TreeNode<string>> { child1, child2 });

        var json = JsonSerializer.Serialize(parent, _options);

        json.ShouldNotBeNull();
        Assert.That(json, Does.Contain(""""Value":"Parent""""));
        Assert.That(json, Does.Contain("Children"));
        Assert.That(json, Does.Contain(""""Value":"Child1""""));
        Assert.That(json, Does.Contain(""""Value":"Child2""""));
    }

    [Test]
    public void Can_serialize_treenode_with_all_properties()
    {
        var child = new TreeNode<string>("Child") { Id = 2 };
        var node = new TreeNode<string>("Parent", new List<TreeNode<string>> { child })
        {
            Id = 1
        };
        node.Metadata["category"] = "test";

        var json = JsonSerializer.Serialize(node, _options);

        json.ShouldNotBeNull();
        Assert.That(json, Does.Contain(@"""Id"":1"));
        Assert.That(json, Does.Contain(@"""Value"":""Parent"""));
        Assert.That(json, Does.Contain("Metadata"));
        Assert.That(json, Does.Contain(@"""category"":""test"""));
        Assert.That(json, Does.Contain("Children"));
        Assert.That(json, Does.Contain(@"""Value"":""Child"""));
    }

    [Test]
    public void Can_roundtrip_simple_treenode()
    {
        var original = new TreeNode<string>("Test");

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<TreeNode<string>>(json, _options);

        deserialized.ShouldNotBeNull();
        deserialized.Value.ShouldEqual(original.Value);
        deserialized.HasChildren.ShouldEqual(original.HasChildren);
    }

    [Test]
    public void Can_roundtrip_complex_treenode()
    {
        var grandchild = new TreeNode<string>("Grandchild") { Id = 3 };
        var child1 = new TreeNode<string>("Child1", new List<TreeNode<string>> { grandchild }) { Id = 2 };
        var child2 = new TreeNode<string>("Child2") { Id = 4 };
        var original = new TreeNode<string>("Root", new List<TreeNode<string>> { child1, child2 })
        {
            Id = 1
        };
        original.Metadata["level"] = 0;
        original.Metadata["category"] = "root";

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<TreeNode<string>>(json, _options);

        deserialized.ShouldNotBeNull();
        ((JsonElement)deserialized.Id).GetInt32().ShouldEqual(1);
        deserialized.Value.ShouldEqual("Root");
        deserialized.Metadata.Count.ShouldEqual(2);
        deserialized.HasChildren.ShouldEqual(true);
        deserialized.Children.Count.ShouldEqual(2);
        deserialized.Children[0].Value.ShouldEqual("Child1");
        ((JsonElement)deserialized.Children[0].Id).GetInt32().ShouldEqual(2);
        deserialized.Children[0].HasChildren.ShouldEqual(true);
        deserialized.Children[0].Children[0].Value.ShouldEqual("Grandchild");
        deserialized.Children[1].Value.ShouldEqual("Child2");
        ((JsonElement)deserialized.Children[1].Id).GetInt32().ShouldEqual(4);
    }

    [Test]
    public void Can_deserialize_treenode_with_integer_type()
    {
        var json = """{"Value":42}""";

        var node = JsonSerializer.Deserialize<TreeNode<int>>(json, _options);

        node.ShouldNotBeNull();
        node.Value.ShouldEqual(42);
    }

    [Test]
    public void Can_serialize_and_deserialize_treenode_with_complex_type()
    {
        var original = new TreeNode<TestData>(new TestData { Name = "Test", Count = 10 })
        {
            Id = "test-1"
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<TreeNode<TestData>>(json, _options);

        deserialized.ShouldNotBeNull();
        deserialized.Value.ShouldNotBeNull();
        deserialized.Value.Name.ShouldEqual("Test");
        deserialized.Value.Count.ShouldEqual(10);
        deserialized.Id.ToString().ShouldEqual("test-1");
    }

    [Test]
    public void Serialization_does_not_include_empty_metadata()
    {
        var node = new TreeNode<string>("Test");

        var json = JsonSerializer.Serialize(node, _options);

        Assert.That(json, Does.Not.Contain("Metadata"));
    }

    [Test]
    public void Serialization_does_not_include_null_id()
    {
        var node = new TreeNode<string>("Test");

        var json = JsonSerializer.Serialize(node, _options);

        Assert.That(json, Does.Not.Contain("Id"));
    }

    private class TestData
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}