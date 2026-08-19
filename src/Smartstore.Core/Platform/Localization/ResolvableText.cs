#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Smartstore.Json.Polymorphy;

namespace Smartstore.Core.Localization;

/// <summary>
/// Represents text whose translated value can be resolved at runtime.
/// </summary>
[JsonConverter(typeof(ResolvableTextJsonConverter))]
public abstract class ResolvableText : IEquatable<ResolvableText>
{
    /// <summary>
    /// Creates resolvable text containing a literal value.
    /// </summary>
    /// <param name="value">The literal text value.</param>
    /// <returns>The resolvable literal text.</returns>
    public static ResolvableText Literal(string value)
        => new LiteralText(value);

    /// <summary>
    /// Creates resolvable text referring to a localization resource.
    /// </summary>
    /// <param name="resourceKey">The localization resource key.</param>
    /// <returns>The resolvable resource text.</returns>
    public static ResolvableText Resource(string resourceKey)
        => new ResourceText(resourceKey);

    /// <summary>
    /// Resolves the localized text value.
    /// </summary>
    /// <param name="localizer">The localizer used to resolve resource-backed text.</param>
    /// <returns>The resolved localized text.</returns>
    public abstract LocalizedString Resolve(Localizer localizer);

    /// <summary>
    /// Determines whether this instance represents the same text source as another instance.
    /// </summary>
    /// <param name="other">The instance to compare.</param>
    /// <returns><see langword="true"/> when type and value are equal; otherwise, <see langword="false"/>.</returns>
    public abstract bool Equals(ResolvableText? other);

    /// <inheritdoc />
    public abstract override int GetHashCode();

    /// <inheritdoc />
    public abstract override string ToString();

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => Equals(obj as ResolvableText);

    /// <summary>
    /// Converts a literal string value to a <see cref="ResolvableText"/>.
    /// </summary>
    /// <param name="value">The literal string value.</param>
    /// <returns>The resolvable literal text.</returns>
    public static implicit operator ResolvableText(string value)
        => Literal(value);

    /// <summary>
    /// Determines whether two resolvable texts represent the same text source.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> when both values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ResolvableText? left, ResolvableText? right)
        => EqualityComparer<ResolvableText>.Default.Equals(left, right);

    /// <summary>
    /// Determines whether two resolvable texts represent different text sources.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> when both values differ; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ResolvableText? left, ResolvableText? right)
        => !(left == right);
}

/// <summary>
/// Converts polymorphic <see cref="ResolvableText"/> values to and from JSON.
/// </summary>
internal sealed class ResolvableTextJsonConverter : JsonConverter<ResolvableText>
{
    /// <inheritdoc />
    public override ResolvableText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.GetString() is not string value || value.IsEmpty())
                throw new JsonException("Resolvable literal text must be a non-empty string.");

            return ResolvableText.Literal(value);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Cannot convert token '{reader.TokenType}' to '{typeof(ResolvableText)}'.");

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (TryGetProperty(root, "kind", out var kind) &&
            kind.ValueKind == JsonValueKind.String &&
            string.Equals(kind.GetString(), "resource", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetProperty(root, "resourceKey", out var resourceKey) ||
                resourceKey.ValueKind != JsonValueKind.String ||
                resourceKey.GetString() is not string value ||
                value.IsEmpty())
            {
                throw new JsonException("Resource text requires a non-empty 'resourceKey'.");
            }

            return ResolvableText.Resource(value);
        }

        return options.DeserializePolymorphic<ResolvableText>(root)
            ?? throw new JsonException($"Cannot deserialize null as '{typeof(ResolvableText)}'.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ResolvableText value, JsonSerializerOptions options)
    {
        if (value is LiteralText literal)
        {
            writer.WriteStringValue(literal.Value);
            return;
        }

        if (value is ResourceText resource)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", "resource");
            writer.WriteString("resourceKey", resource.ResourceKey);
            writer.WriteEndObject();
            return;
        }

        options.SerializePolymorphic(writer, value, typeof(ResolvableText));
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
