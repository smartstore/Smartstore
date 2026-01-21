#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;
using Smartstore.Json.Polymorphy;

namespace Smartstore.Json.Converters;

/// <summary>
/// System.Text.Json converter for <see cref="RouteValueDictionary"/>.
/// Writes as <see cref="IDictionary{TKey, TValue}"/> (<c>IDictionary&lt;string, object?&gt;</c>) and reads back the same
/// before creating a new <see cref="RouteValueDictionary"/> instance.
/// </summary>
public sealed class RouteValueDictionaryJsonConverter : JsonConverter<RouteValueDictionary?>
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(RouteValueDictionary);

    public override RouteValueDictionary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        // Read as dictionary slot (object values) first, then materialize RouteValueDictionary.
        var raw = options.DeserializePolymorphic<IDictionary<string, object?>>(ref reader);
        return new RouteValueDictionary(raw);
    }

    public override void Write(Utf8JsonWriter writer, RouteValueDictionary? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        // Write as plain dictionary slot (object values).
        options.SerializePolymorphic(writer, value.ToDictionary());
    }
}