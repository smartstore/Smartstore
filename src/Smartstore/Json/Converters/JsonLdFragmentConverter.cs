#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Converters;

/// <summary>
/// Serializes a <see cref="JsonLdFragment"/> as its underlying <see cref="JsonObject"/> data node,
/// so that <see cref="JsonLdFragment"/> instances embedded in anonymous objects serialize correctly.
/// </summary>
internal sealed class JsonLdFragmentConverter : JsonConverter<JsonLdFragment>
{
    public override JsonLdFragment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, JsonLdFragment value, JsonSerializerOptions options)
        => ((JsonObject)value).WriteTo(writer, options);
}
