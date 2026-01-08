using System.Text.Json;
using NUnit.Framework;
using Smartstore;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Common;

[TestFixture]
public class SemanticVersionJsonConverterTests
{
    private readonly JsonSerializerOptions _options = new();

    [TestCase("1.2.0.0", 1, 2, 0, 0)]
    [TestCase("2.0.0-rc.1", 2, 0, 0)]
    [TestCase("2.0.0-alpha+exp.sha.5114f85", 2, 0, 0)]
    public void Can_deserialize_semantic_version_from_json(string versionString, int major, int minor, int build, int revision = 0)
    {
        var json = JsonSerializer.Serialize(versionString);
        var result = JsonSerializer.Deserialize<SemanticVersion>(json, _options);

        result.ShouldNotBeNull();
        result.Version.Major.ShouldEqual(major);
        result.Version.Minor.ShouldEqual(minor);
        result.Version.Build.ShouldEqual(build);
    }

    [Test]
    public void Can_deserialize_null_semantic_version_from_json()
    {
        var json = "null";
        var result = JsonSerializer.Deserialize<SemanticVersion>(json, _options);

        result.ShouldBeNull();
    }

    [TestCase("1.2.0.0")]
    [TestCase("2.0.0-rc.1")]
    [TestCase("2.0.0-alpha+exp.sha.5114f85")]
    public void Can_serialize_semantic_version_to_json(string versionString)
    {
        var version = SemanticVersion.Parse(versionString);
        var json = JsonSerializer.Serialize(version, _options);

        json.ShouldNotBeNull();
        json.Contains(version.ToString()).ShouldBeTrue();
    }

    [Test]
    public void Can_serialize_null_semantic_version_to_json()
    {
        SemanticVersion version = null;
        var json = JsonSerializer.Serialize(version, _options);

        json.ShouldEqual("null");
    }

    [TestCase("1.2.0.0")]
    [TestCase("2.0.0-rc.1")]
    [TestCase("2.0.0-alpha+exp.sha.5114f85")]
    public void Can_roundtrip_semantic_version_through_json(string versionString)
    {
        var original = SemanticVersion.Parse(versionString);
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<SemanticVersion>(json, _options);

        deserialized.ShouldNotBeNull();
        deserialized.ShouldEqual(original);
    }

    [Test]
    public void Returns_null_when_deserializing_invalid_version_string()
    {
        var json = JsonSerializer.Serialize("invalid.version.string");
        var result = JsonSerializer.Deserialize<SemanticVersion>(json, _options);

        result.ShouldBeNull();
    }

    [Test]
    public void Returns_null_when_deserializing_empty_string()
    {
        var json = JsonSerializer.Serialize(string.Empty);
        var result = JsonSerializer.Deserialize<SemanticVersion>(json, _options);

        result.ShouldBeNull();
    }

    [Test]
    public void Returns_null_when_deserializing_non_string_token()
    {
        var json = "123";
        var result = JsonSerializer.Deserialize<SemanticVersion>(json, _options);

        result.ShouldBeNull();
    }
}