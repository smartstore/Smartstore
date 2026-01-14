#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Smartstore.Json;

namespace Smartstore.Tests;

[TestFixture]
public class JsonTests
{
    private readonly JsonSerializerOptions _jsonOptions = SmartJsonOptions.Default.Create(o =>
    {
        o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    });

    [Test]
    public void CanSerialize()
    {
        var obj = CreateTestObject();

        var json = JsonSerializer.Serialize(obj, _jsonOptions);
        var node = JsonSerializer.Deserialize<JsonNode>(json, _jsonOptions);
    }

    [Test]
    public void NsjPolymorphyTest()
    {
        var root = CreateTestRootObject();
        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Arrays
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(root, settings);
        var root2 = Newtonsoft.Json.JsonConvert.DeserializeObject<RootClass>(json, settings);
    }

    [Test]
    public void StjPolymorphyTest()
    {
        var root = CreateTestRootObject();

        var json = JsonSerializer.Serialize(root, _jsonOptions);
        var root2 = JsonSerializer.Deserialize<RootClass>(json, _jsonOptions);
    }

    private RootClass CreateTestRootObject()
    {
        var list = new List<MapClass1> { CreateTestObject() };
        var list2 = new List<object> { CreateTestObject() };

        var dict = new CustomProps
        {
            ["string"] = "String",
            ["bool"] = true,
            ["obj"] = CreateTestObject(),
            ["typed_list"] = list,
            ["untyped_list"] = list2
        };

        return new RootClass { Name = "MyName", Properties = dict, Data = list };
    }

    private MapClass1 CreateTestObject()
    {
        return new MapClass1
        {
            Prop1 = "Value1",
            Prop2 = "Value2",
            //Prop3 = "Value3",
            //Prop4 = 4.5f,
            Prop4 = 0f,
            //Prop5 = [ConsoleKey.A, ConsoleKey.B],
            Prop6 = false,
            Address = new MapNestedClass { FirstName = "John", LastName = "Doe", Age = 18 }
        };
    }

    class RootClass
    {
        public string? Name { get; set; }

        [DefaultValue("hello")]
        public string? Prop1 { get; set; } = "hello";

        [DefaultValue(0.5f)]
        public float Prop2 { get; set; } = 0.5f;

        [DefaultValue(true)]
        public bool Prop3 { get; set; } = true;

        [Polymorphic]
        public object? Data { get; set; }

        public CustomProps? Properties { get; set; }
    }

    [Polymorphic(WrapDictionaryArrays = true)]
    class CustomProps : Dictionary<string, object>
    {
    }
}
