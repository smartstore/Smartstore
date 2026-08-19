#nullable enable

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Smartstore.Core.Localization;
using Smartstore.Json;

namespace Smartstore.Core.Tests.Platform.Localization;

/// <summary>
/// Verifies literal and resource-backed resolvable text.
/// </summary>
[TestFixture]
public sealed class ResolvableTextTests
{
    /// <summary>
    /// Verifies that an implicitly converted string is treated as a literal value.
    /// </summary>
    [Test]
    public void Implicitly_Converts_String_To_Literal()
    {
        ResolvableText value = "Umami";

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.TypeOf<LiteralText>());
            Assert.That(value.Resolve(NullLocalizer.Instance), Is.EqualTo("Umami"));
        });
    }

    /// <summary>
    /// Verifies that resource-backed strings are resolved with the supplied localizer.
    /// </summary>
    [Test]
    public void Resolves_Resource_String()
    {
        var value = ResolvableText.Resource("Dashboard.Groups.Sales");
        Localizer localizer = (key, _) => new LocalizedString($"Resolved:{key}");

        var result = value.Resolve(localizer);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.TypeOf<ResourceText>());
            Assert.That(result, Is.EqualTo("Resolved:Dashboard.Groups.Sales"));
        });
    }

    /// <summary>
    /// Verifies that equality includes both source type and value.
    /// </summary>
    [Test]
    public void Compares_Source_Type_And_Value()
    {
        var literal1 = ResolvableText.Literal("Sales");
        var literal2 = ResolvableText.Literal("Sales");
        var resource = ResolvableText.Resource("Sales");

        Assert.Multiple(() =>
        {
            Assert.That(literal1, Is.EqualTo(literal2));
            Assert.That(literal1 == literal2, Is.True);
            Assert.That(literal1, Is.Not.EqualTo(resource));
        });
    }

    /// <summary>
    /// Verifies that literal text uses the compact JSON string representation.
    /// </summary>
    [Test]
    public void Json_Roundtrips_Literal_As_String()
    {
        ResolvableText source = "Umami";

        var json = JsonSerializer.Serialize(source);
        var result = JsonSerializer.Deserialize<ResolvableText>(json);

        Assert.Multiple(() =>
        {
            Assert.That(json, Is.EqualTo("\"Umami\""));
            Assert.That(result, Is.EqualTo(source));
            Assert.That(result, Is.TypeOf<LiteralText>());
        });
    }

    /// <summary>
    /// Verifies that resource text uses a stable discriminated JSON object.
    /// </summary>
    [Test]
    public void Json_Roundtrips_Resource_As_Object()
    {
        var source = ResolvableText.Resource("Dashboard.Groups.Sales");

        var json = JsonSerializer.Serialize(source);
        var result = JsonSerializer.Deserialize<ResolvableText>(json);

        Assert.Multiple(() =>
        {
            Assert.That(json, Is.EqualTo("{\"kind\":\"resource\",\"resourceKey\":\"Dashboard.Groups.Sales\"}"));
            Assert.That(result, Is.EqualTo(source));
            Assert.That(result, Is.TypeOf<ResourceText>());
        });
    }

    /// <summary>
    /// Verifies that the converter works with Smartstore's preconfigured serializer options.
    /// </summary>
    [Test]
    public void Json_Works_With_Smartstore_Options()
    {
        var source = ResolvableText.Resource("Dashboard.Groups.Sales");

        var json = JsonSerializer.Serialize(source, SmartJsonOptions.CamelCased);
        var result = JsonSerializer.Deserialize<ResolvableText>(json, SmartJsonOptions.CamelCased);

        Assert.That(result, Is.EqualTo(source));
    }

    /// <summary>
    /// Verifies that nullable resolvable text uses STJ's standard null representation.
    /// </summary>
    [Test]
    public void Json_Roundtrips_Null()
    {
        var json = JsonSerializer.Serialize<ResolvableText?>(null);
        var result = JsonSerializer.Deserialize<ResolvableText?>(json);

        Assert.Multiple(() =>
        {
            Assert.That(json, Is.EqualTo("null"));
            Assert.That(result, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that resource properties are read case-insensitively and unknown properties are ignored.
    /// </summary>
    [Test]
    public void Json_Tolerates_Property_Casing_And_Unknown_Properties()
    {
        const string json = "{\"KIND\":\"RESOURCE\",\"unknown\":{\"nested\":true},\"RESOURCEKEY\":\"Dashboard.Groups.Sales\"}";

        var result = JsonSerializer.Deserialize<ResolvableText>(json);

        Assert.That(result, Is.EqualTo(ResolvableText.Resource("Dashboard.Groups.Sales")));
    }

    /// <summary>
    /// Verifies that malformed resource representations are rejected.
    /// </summary>
    [TestCase("{}")]
    [TestCase("{\"kind\":\"unknown\",\"resourceKey\":\"Key\"}")]
    [TestCase("{\"kind\":\"resource\"}")]
    [TestCase("{\"kind\":\"resource\",\"resourceKey\":\"\"}")]
    public void Json_Rejects_Invalid_Representations(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ResolvableText>(json));
    }

    /// <summary>
    /// Verifies that custom implementations can use their own scalar JSON converter.
    /// </summary>
    [Test]
    public void Json_Roundtrips_Custom_Implementation_With_Own_Converter()
    {
        ResolvableText source = new CustomText("Value");

        var json = JsonSerializer.Serialize(source);
        var result = JsonSerializer.Deserialize<ResolvableText>(json);

        using var document = JsonDocument.Parse(json);

        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("$type").GetString(), Does.Contain(nameof(CustomText)));
            Assert.That(document.RootElement.GetProperty("$value").GetString(), Is.EqualTo("custom:Value"));
            Assert.That(result, Is.EqualTo(source));
            Assert.That(result, Is.TypeOf<CustomText>());
        });
    }

    /// <summary>
    /// Represents a custom resolvable text implementation used to verify extensibility.
    /// </summary>
    [JsonConverter(typeof(CustomTextJsonConverter))]
    private sealed class CustomText : ResolvableText
    {
        public CustomText(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the custom text value.
        /// </summary>
        public string Value { get; }

        /// <inheritdoc />
        public override string Resolve(Localizer localizer, params object?[] args)
            => Value;

        /// <inheritdoc />
        public override bool Equals(ResolvableText? other)
            => other is CustomText text && string.Equals(Value, text.Value, StringComparison.Ordinal);

        /// <inheritdoc />
        public override int GetHashCode()
            => HashCode.Combine(typeof(CustomText), Value);

        /// <inheritdoc />
        public override string ToString()
            => Value;
    }

    /// <summary>
    /// Converts <see cref="CustomText"/> values to and from a custom scalar JSON representation.
    /// </summary>
    private sealed class CustomTextJsonConverter : JsonConverter<CustomText>
    {
        /// <inheritdoc />
        public override CustomText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value is null || !value.StartsWith("custom:", StringComparison.Ordinal))
                throw new JsonException("Invalid custom text value.");

            return new CustomText(value["custom:".Length..]);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, CustomText value, JsonSerializerOptions options)
            => writer.WriteStringValue($"custom:{value.Value}");
    }
}
