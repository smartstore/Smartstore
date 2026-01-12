using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using NUnit.Framework;
using Smartstore.Http;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Http
{
    [TestFixture]
    public class RouteInfoTests
    {
        private JsonSerializerOptions _options;

        [SetUp]
        public void Setup()
        {
            _options = new JsonSerializerOptions();
        }

        #region StjRouteInfoConverter Read Tests

        [Test]
        public void Can_deserialize_null()
        {
            var json = "null";
            var result = JsonSerializer.Deserialize<RouteInfo>(json, _options);
            result.ShouldBeNull();
        }

        [Test]
        public void Can_deserialize_complete_routeinfo()
        {
            var json = """
            {
                "Action": "Edit",
                "Controller": "Product",
                "RouteValues": {
                    "id": 123,
                    "area": "Admin"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<RouteInfo>(json, _options);

            result.ShouldNotBeNull();
            result.Action.ShouldEqual("Edit");
            result.Controller.ShouldEqual("Product");
            result.RouteValues.ShouldNotBeNull();
            result.RouteValues.Count.ShouldEqual(2);
            ((JsonElement)result.RouteValues["id"]).GetInt32().ShouldEqual(123);
            result.RouteValues["area"].ToString().ShouldEqual("Admin");
        }

        [Test]
        public void Can_deserialize_routeinfo_without_controller()
        {
            var json = """
            {
                "Action": "Index",
                "RouteValues": {
                    "page": 1
                }
            }
            """;

            var result = JsonSerializer.Deserialize<RouteInfo>(json, _options);

            result.ShouldNotBeNull();
            result.Action.ShouldEqual("Index");
            result.Controller.ShouldBeNull();
            result.RouteValues.ShouldNotBeNull();
            result.RouteValues.Count.ShouldEqual(1);
            ((JsonElement)result.RouteValues["page"]).GetInt32().ShouldEqual(1);
        }

        [Test]
        public void Can_deserialize_routeinfo_with_empty_routevalues()
        {
            var json = """
            {
                "Action": "List",
                "Controller": "Customer",
                "RouteValues": {}
            }
            """;

            var result = JsonSerializer.Deserialize<RouteInfo>(json, _options);

            result.ShouldNotBeNull();
            result.Action.ShouldEqual("List");
            result.Controller.ShouldEqual("Customer");
            result.RouteValues.ShouldNotBeNull();
            result.RouteValues.Count.ShouldEqual(0);
        }

        [Test]
        public void Can_deserialize_routeinfo_without_routevalues()
        {
            var json = """
            {
                "Action": "Create",
                "Controller": "Order"
            }
            """;

            var result = JsonSerializer.Deserialize<RouteInfo>(json, _options);

            result.ShouldNotBeNull();
            result.Action.ShouldEqual("Create");
            result.Controller.ShouldEqual("Order");
            result.RouteValues.ShouldNotBeNull();
            result.RouteValues.Count.ShouldEqual(0);
        }

        [Test]
        public void Can_deserialize_routeinfo_case_insensitive()
        {
            var json = """
            {
                "action": "Delete",
                "controller": "Category",
                "routevalues": {
                    "id": 456
                }
            }
            """;

            var result = JsonSerializer.Deserialize<RouteInfo>(json, _options);

            result.ShouldNotBeNull();
            result.Action.ShouldEqual("Delete");
            result.Controller.ShouldEqual("Category");
            result.RouteValues.ShouldNotBeNull();
            ((JsonElement)result.RouteValues["id"]).GetInt32().ShouldEqual(456);
        }

        [Test]
        public void Can_deserialize_routeinfo_with_complex_routevalues()
        {
            var json = """
            {
                "Action": "Search",
                "Controller": "Catalog",
                "RouteValues": {
                    "query": "test",
                    "categoryId": 10,
                    "priceMin": 99.99,
                    "inStock": true
                }
            }
            """;

            var result = JsonSerializer.Deserialize<RouteInfo>(json, _options);

            result.ShouldNotBeNull();
            result.Action.ShouldEqual("Search");
            result.Controller.ShouldEqual("Catalog");
            result.RouteValues.ShouldNotBeNull();
            result.RouteValues.Count.ShouldEqual(4);
            result.RouteValues["query"].ToString().ShouldEqual("test");
            ((JsonElement)result.RouteValues["categoryId"]).GetInt32().ShouldEqual(10);
            result.RouteValues["priceMin"].ToString().ShouldEqual("99.99");
            ((JsonElement)result.RouteValues["inStock"]).GetBoolean().ShouldEqual(true);
        }

        #endregion

        #region StjRouteInfoConverter Write Tests

        [Test]
        public void Can_serialize_complete_routeinfo()
        {
            var routeValues = new RouteValueDictionary
            {
                { "id", 123 },
                { "area", "Admin" }
            };
            var routeInfo = new RouteInfo("Edit", "Product", routeValues);

            var json = JsonSerializer.Serialize(routeInfo, _options);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("Action").GetString().ShouldEqual("Edit");
            root.GetProperty("Controller").GetString().ShouldEqual("Product");
            root.GetProperty("RouteValues").GetProperty("id").GetInt32().ShouldEqual(123);
            root.GetProperty("RouteValues").GetProperty("area").GetString().ShouldEqual("Admin");
        }

        [Test]
        public void Can_serialize_routeinfo_without_controller()
        {
            var routeValues = new RouteValueDictionary { { "page", 1 } };
            var routeInfo = new RouteInfo("Index", routeValues);

            var json = JsonSerializer.Serialize(routeInfo, _options);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("Action").GetString().ShouldEqual("Index");
            root.TryGetProperty("Controller", out _).ShouldBeFalse();
            root.GetProperty("RouteValues").GetProperty("page").GetInt32().ShouldEqual(1);
        }

        [Test]
        public void Can_serialize_routeinfo_with_empty_routevalues()
        {
            var routeInfo = new RouteInfo("List", "Customer", new RouteValueDictionary());

            var json = JsonSerializer.Serialize(routeInfo, _options);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("Action").GetString().ShouldEqual("List");
            root.GetProperty("Controller").GetString().ShouldEqual("Customer");
            root.GetProperty("RouteValues").EnumerateObject().MoveNext().ShouldBeFalse();
        }

        [Test]
        public void Can_roundtrip_routeinfo()
        {
            var routeValues = new RouteValueDictionary
            {
                { "id", 789 },
                { "name", "TestProduct" },
                { "price", 49.99 }
            };
            var original = new RouteInfo("Details", "Product", routeValues);

            var json = JsonSerializer.Serialize(original, _options);
            var deserialized = JsonSerializer.Deserialize<RouteInfo>(json, _options);

            deserialized.ShouldNotBeNull();
            deserialized.Action.ShouldEqual(original.Action);
            deserialized.Controller.ShouldEqual(original.Controller);
            deserialized.RouteValues.Count.ShouldEqual(original.RouteValues.Count);
            ((JsonElement)deserialized.RouteValues["id"]).GetInt32().ShouldEqual(789);
            deserialized.RouteValues["name"].ToString().ShouldEqual("TestProduct");
            deserialized.RouteValues["price"].ToString().ShouldEqual("49.99");
        }

        #endregion

        #region RouteInfo Constructor Tests

        [Test]
        public void Can_create_from_clone()
        {
            var original = new RouteInfo("Edit", "Product", new RouteValueDictionary { { "id", 1 } });
            var cloned = new RouteInfo(original);

            cloned.Action.ShouldEqual(original.Action);
            cloned.Controller.ShouldEqual(original.Controller);
            cloned.RouteValues.Count.ShouldEqual(original.RouteValues.Count);
            cloned.RouteValues["id"].ShouldEqual(1);
            ReferenceEquals(cloned.RouteValues, original.RouteValues).ShouldBeFalse();
        }

        [Test]
        public void Can_create_with_action_and_object_routevalues()
        {
            var routeInfo = new RouteInfo("Index", new { page = 1, sort = "name" });

            routeInfo.Action.ShouldEqual("Index");
            routeInfo.Controller.ShouldBeNull();
            routeInfo.RouteValues["page"].ShouldEqual(1);
            routeInfo.RouteValues["sort"].ShouldEqual("name");
        }

        [Test]
        public void Can_create_with_action_controller_and_object_routevalues()
        {
            var routeInfo = new RouteInfo("List", "Customer", new { area = "Admin" });

            routeInfo.Action.ShouldEqual("List");
            routeInfo.Controller.ShouldEqual("Customer");
            routeInfo.RouteValues["area"].ShouldEqual("Admin");
        }

        [Test]
        public void Should_throw_when_action_is_empty()
        {
            Assert.Throws<ArgumentException>(() => new RouteInfo("", "Product", new RouteValueDictionary()));
        }

        [Test]
        public void Should_throw_when_routevalues_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new RouteInfo("Index", "Home", (RouteValueDictionary)null));
        }

        #endregion
    }
}