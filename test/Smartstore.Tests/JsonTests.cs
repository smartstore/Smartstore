//using System;
//using System.Text.Json;
//using System.Text.Json.Nodes;
//using NUnit.Framework;
//using Smartstore.Json;

//namespace Smartstore.Tests
//{
//    [TestFixture]
//    public class JsonTests
//    {
//        [Test]
//        public void CanSerialize()
//        {
//            var jsonOptions = SmartJsonOptions.Default;
//            var obj = new MapClass1
//            {
//                Prop1 = "Value1",
//                Prop2 = "Value2",
//                Prop3 = "Value3",
//                Prop4 = 4.5f,
//                Prop5 = [ConsoleKey.A, ConsoleKey.B],
//                Address = new MapNestedClass { FirstName = "John", LastName = "Doe", Age = 18 }
//            };

//            var json = JsonSerializer.Serialize(obj, jsonOptions);
//            var node = JsonSerializer.Deserialize<JsonNode>(json, jsonOptions);
//        }
//    }
//}
