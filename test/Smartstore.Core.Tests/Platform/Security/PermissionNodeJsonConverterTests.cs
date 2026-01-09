using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using Smartstore.Core.Security;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Platform.Security
{
    [TestFixture]
    public class IPermissionNodeStjConverterTests
    {
        private JsonSerializerOptions _options;

        [SetUp]
        public void Setup()
        {
            _options = new JsonSerializerOptions
            {
                Converters = { new IPermissionNodeStjConverter() }
            };
        }

        [Test]
        public void Can_deserialize_permission_node()
        {
            var json = """{"PermissionRecordId":1,"SystemName":"Test.Permission","Allow":true}""";

            var result = JsonSerializer.Deserialize<IPermissionNode>(json, _options);

            result.ShouldNotBeNull();
            result.PermissionRecordId.ShouldEqual(1);
            result.SystemName.ShouldEqual("Test.Permission");
            result.Allow.ShouldEqual(true);
        }

        [Test]
        public void Can_deserialize_permission_node_with_null_allow()
        {
            var json = """{"PermissionRecordId":5,"SystemName":"Test.NullPermission","Allow":null}""";

            var result = JsonSerializer.Deserialize<IPermissionNode>(json, _options);

            result.ShouldNotBeNull();
            result.PermissionRecordId.ShouldEqual(5);
            result.SystemName.ShouldEqual("Test.NullPermission");
            result.Allow.ShouldBeNull();
        }

        [Test]
        public void Can_deserialize_permission_node_with_false_allow()
        {
            var json = """{"PermissionRecordId":10,"SystemName":"Test.DeniedPermission","Allow":false}""";

            var result = JsonSerializer.Deserialize<IPermissionNode>(json, _options);

            result.ShouldNotBeNull();
            result.PermissionRecordId.ShouldEqual(10);
            result.SystemName.ShouldEqual("Test.DeniedPermission");
            result.Allow.ShouldEqual(false);
        }

        [Test]
        public void Can_serialize_permission_node()
        {
            var node = new PermissionNode
            {
                PermissionRecordId = 1,
                SystemName = "Test.Permission",
                Allow = true
            };

            var json = JsonSerializer.Serialize<IPermissionNode>(node, _options);

            Assert.That(json, Is.Not.Empty);
            Assert.That(json, Does.Contain("\"PermissionRecordId\":1"));
            Assert.That(json, Does.Contain("\"SystemName\":\"Test.Permission\""));
            Assert.That(json, Does.Contain("\"Allow\":true"));
        }

        [Test]
        public void Can_serialize_permission_node_with_null_allow()
        {
            var node = new PermissionNode
            {
                PermissionRecordId = 2,
                SystemName = "Test.NullPermission",
                Allow = null
            };

            var json = JsonSerializer.Serialize<IPermissionNode>(node, _options);

            Assert.That(json, Is.Not.Empty);
            Assert.That(json, Does.Contain("\"PermissionRecordId\":2"));
            Assert.That(json, Does.Contain("\"SystemName\":\"Test.NullPermission\""));
            Assert.That(json, Does.Contain("\"Allow\":null"));
        }

        [Test]
        public void Can_roundtrip_permission_node()
        {
            var originalNode = new PermissionNode
            {
                PermissionRecordId = 42,
                SystemName = "Catalog.Products.Read",
                Allow = true
            };

            var json = JsonSerializer.Serialize<IPermissionNode>(originalNode, _options);
            var deserializedNode = JsonSerializer.Deserialize<IPermissionNode>(json, _options);

            deserializedNode.ShouldNotBeNull();
            deserializedNode.PermissionRecordId.ShouldEqual(originalNode.PermissionRecordId);
            deserializedNode.SystemName.ShouldEqual(originalNode.SystemName);
            deserializedNode.Allow.ShouldEqual(originalNode.Allow);
        }

        [Test]
        public void Can_roundtrip_permission_node_with_null_allow()
        {
            var originalNode = new PermissionNode
            {
                PermissionRecordId = 99,
                SystemName = "Admin.System.Maintenance",
                Allow = null
            };

            var json = JsonSerializer.Serialize<IPermissionNode>(originalNode, _options);
            var deserializedNode = JsonSerializer.Deserialize<IPermissionNode>(json, _options);

            deserializedNode.ShouldNotBeNull();
            deserializedNode.PermissionRecordId.ShouldEqual(originalNode.PermissionRecordId);
            deserializedNode.SystemName.ShouldEqual(originalNode.SystemName);
            deserializedNode.Allow.ShouldBeNull();
        }

        [Test]
        public void Can_deserialize_empty_json_object()
        {
            var json = "{}";

            var result = JsonSerializer.Deserialize<IPermissionNode>(json, _options);

            result.ShouldNotBeNull();
            result.PermissionRecordId.ShouldEqual(0);
            result.SystemName.ShouldBeNull();
            result.Allow.ShouldBeNull();
        }

        [Test]
        public void Can_serialize_and_deserialize_collection()
        {
            var nodes = new List<IPermissionNode>
            {
                new PermissionNode { PermissionRecordId = 1, SystemName = "Permission.One", Allow = true },
                new PermissionNode { PermissionRecordId = 2, SystemName = "Permission.Two", Allow = false },
                new PermissionNode { PermissionRecordId = 3, SystemName = "Permission.Three", Allow = null }
            };

            var json = JsonSerializer.Serialize(nodes, _options);
            var deserializedNodes = JsonSerializer.Deserialize<List<IPermissionNode>>(json, _options);

            deserializedNodes.ShouldNotBeNull();
            deserializedNodes.Count.ShouldEqual(3);

            deserializedNodes[0].PermissionRecordId.ShouldEqual(1);
            deserializedNodes[0].SystemName.ShouldEqual("Permission.One");
            deserializedNodes[0].Allow.ShouldEqual(true);

            deserializedNodes[1].PermissionRecordId.ShouldEqual(2);
            deserializedNodes[1].SystemName.ShouldEqual("Permission.Two");
            deserializedNodes[1].Allow.ShouldEqual(false);

            deserializedNodes[2].PermissionRecordId.ShouldEqual(3);
            deserializedNodes[2].SystemName.ShouldEqual("Permission.Three");
            deserializedNodes[2].Allow.ShouldBeNull();
        }
    }
}