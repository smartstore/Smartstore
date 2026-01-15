#nullable enable

using System.Buffers;
using System.Collections;
using System.Text;
using System.Text.Json;

namespace Smartstore.Json.Polymorphy;

public static class PolymorphSerializationExtensions
{
    public static string SerializePolymorphicObject(
        this JsonSerializerOptions options,
        object? value,
        bool wrapArrays = false)
    {
        Guard.NotNull(options);

        if (value is null)
            return "null";

        var poly = wrapArrays ? PolymorphyOptions.DefaultWithArrays : PolymorphyOptions.Default;

        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            PolymorphyCodec.WriteObjectSlot(writer, value, options, poly);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static object? DeserializePolymorphicObject(this JsonSerializerOptions options, string json)
    {
        Guard.NotNull(options);
        Guard.NotNull(json);

        if (json.Length <= 8 && json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        using var doc = JsonDocument.Parse(json);
        return PolymorphyCodec.Read(doc.RootElement, typeof(object), options, PolymorphyOptions.Default);
    }

    public static string SerializePolymorphicDictionary(
        this JsonSerializerOptions options,
        IDictionary<string, object?>? dictionary,
        bool wrapArrays = false)
    {
        Guard.NotNull(options);

        if (dictionary is null)
            return "null";

        var poly = wrapArrays ? PolymorphyOptions.DefaultWithArrays : PolymorphyOptions.Default;

        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            PolymorphyCodec.WriteDictionarySlot(writer, dictionary, options, poly);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static IDictionary<string, object?>? DeserializePolymorphicDictionary(this JsonSerializerOptions options,string json)
    {
        Guard.NotNull(options);
        Guard.NotNull(json);

        if (json.Length <= 8 && json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        using var doc = JsonDocument.Parse(json);

        // Read as object-root (untyped) with $type support.
        var obj = PolymorphyCodec.Read(doc.RootElement, typeof(object), options, PolymorphyOptions.Default);

        if (obj is IDictionary<string, object?> dict)
            return dict;

        if (obj is IDictionary nongeneric)
        {
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (DictionaryEntry de in nongeneric)
                result[Convert.ToString(de.Key) ?? string.Empty] = de.Value;
            return result;
        }

        throw new JsonException($"JSON is not a dictionary/object. Got '{obj?.GetType().FullName ?? "null"}'.");
    }

    public static string SerializePolymorphicList(
        this JsonSerializerOptions options,
        IEnumerable? list,
        bool wrapArrays = false)
    {
        Guard.NotNull(options);

        if (list is null)
            return "null";

        var poly = wrapArrays ? PolymorphyOptions.DefaultWithArrays : PolymorphyOptions.Default;

        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            PolymorphyCodec.WriteListSlot(writer, list, options, poly);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static object? DeserializePolymorphicList(this JsonSerializerOptions options, string json)
    {
        Guard.NotNull(options);
        Guard.NotNull(json);

        if (json.Length <= 8 && json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        using var doc = JsonDocument.Parse(json);

        // Declared type is object so raw arrays become List<object?> and wrapped arrays are honored.
        var obj = PolymorphyCodec.Read(doc.RootElement, typeof(object), options, PolymorphyOptions.Default);

        // Caller can cast to List<object?>, object[], HashSet<object?> etc. based on their needs.
        return obj;
    }
}
