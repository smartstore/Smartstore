#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Smartstore.Json;

namespace Smartstore.Tests;

[TestFixture]
public class JsonTests
{
    [Test]
    public void CanSerialize()
    {
        var jsonOptions = SmartJsonOptions.Default;
        var obj = CreateTestObject();

        var json = JsonSerializer.Serialize(obj, jsonOptions);
        var node = JsonSerializer.Deserialize<JsonNode>(json, jsonOptions);
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
        var options = SmartJsonOptions.Default;

        var json = JsonSerializer.Serialize(root, options);
        var root2 = JsonSerializer.Deserialize<RootClass>(json, options);
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
            Prop3 = "Value3",
            Prop4 = 4.5f,
            //Prop5 = [ConsoleKey.A, ConsoleKey.B],
            Address = new MapNestedClass { FirstName = "John", LastName = "Doe", Age = 18 }
        };
    }

    class RootClass
    {
        public string? Name { get; set; }

        [Polymorphic]
        public object? Data { get; set; }

        public CustomProps? Properties { get; set; }
    }

    [Polymorphic(WrapDictionaryArrays = true)]
    class CustomProps : Dictionary<string, object>
    {
    }
}
