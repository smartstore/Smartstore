#nullable enable

using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Smartstore.Json.Polymorphy;

public static class PolymorphSerializationExtensions
{
    const string ValueName = "Value";

    internal sealed class DictionaryStub<TValue>
    {
        [Polymorphic(WrapArrays = false)]
        public IDictionary<string, TValue?>? Value { get; set; }
    }

    internal sealed class DictionaryStubWithArrays<TValue>
    {
        [Polymorphic(WrapArrays = true)]
        public IDictionary<string, TValue?>? Value { get; set; }
    }

    internal sealed class ListStub<TElement>
    {
        [Polymorphic(WrapArrays = false)]
        public List<TElement?>? Value { get; set; }
    }

    internal sealed class ListStubWithArrays<TElement>
    {
        [Polymorphic(WrapArrays = true)]
        public List<TElement?>? Value { get; set; }
    }

    extension(JsonSerializerOptions options)
    {
        // ----------------------------
        // Dictionary - Serialize
        // ----------------------------

        public string SerializePolymorphicDictionary<TValue>(
            IDictionary<string, TValue?>? dictionary,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            EnsurePolymorphType<TValue>();

            return dictionary is null
                ? "null"
                : Encoding.UTF8.GetString(SerializePolymorphicDictionaryToUtf8Bytes(options, dictionary, wrapArrays));
        }

        public byte[] SerializePolymorphicDictionaryToUtf8Bytes<TValue>(
            IDictionary<string, TValue?>? dictionary,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            EnsurePolymorphType<TValue>();

            if (dictionary is null)
                return Encoding.UTF8.GetBytes("null");

            var buffer = new ArrayBufferWriter<byte>(256);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                WritePolymorphicDictionary(options, writer, dictionary, wrapArrays);
            }

            return buffer.WrittenSpan.ToArray();
        }

        public void WritePolymorphicDictionary<TValue>(
            Utf8JsonWriter writer,
            IDictionary<string, TValue?>? dictionary,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            Guard.NotNull(writer);
            EnsurePolymorphType<TValue>();

            if (dictionary is null)
            {
                writer.WriteNullValue();
                return;
            }

            var el = SerializePolymorphicDictionaryToElement(options, dictionary, wrapArrays);
            el.WriteTo(writer);
        }

        public JsonElement SerializePolymorphicDictionaryToElement<TValue>(
            IDictionary<string, TValue?>? dictionary,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            EnsurePolymorphType<TValue>();

            if (dictionary is null)
            {
                using var doc = JsonDocument.Parse("null");
                return doc.RootElement.Clone();
            }

            object root = wrapArrays
                ? new DictionaryStubWithArrays<TValue> { Value = dictionary }
                : new DictionaryStub<TValue> { Value = dictionary };

            var el = JsonSerializer.SerializeToElement(root, root.GetType(), options);

            if (!el.TryGetProperty(ValueName, out var vEl))
                throw new JsonException($"Internal envelope property '{ValueName}' missing.");

            return vEl.Clone();
        }

        // ----------------------------
        // Dictionary - Deserialize
        // ----------------------------

        public IDictionary<string, object?>? DeserializePolymorphicDictionary(string json)
            => DeserializePolymorphicDictionary<object?>(options, json);

        public IDictionary<string, TValue?>? DeserializePolymorphicDictionary<TValue>(string json)
        {
            Guard.NotNull(options);
            Guard.NotNull(json);
            EnsurePolymorphType<TValue>();

            if (json.Length <= 8 && json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            using var doc = JsonDocument.Parse(json);
            return ReadPolymorphicDictionaryFromRootElement<TValue>(options, doc.RootElement);
        }

        public IDictionary<string, object?>? ReadPolymorphicDictionary(ref Utf8JsonReader reader)
            => ReadPolymorphicDictionary<object?>(options, ref reader);

        public IDictionary<string, TValue?>? ReadPolymorphicDictionary<TValue>(ref Utf8JsonReader reader)
        {
            Guard.NotNull(options);
            EnsurePolymorphType<TValue>();

            if (reader.TokenType == JsonTokenType.Null)
                return null;
            
            using var doc = JsonDocument.ParseValue(ref reader);
            return ReadPolymorphicDictionaryFromRootElement<TValue>(options, doc.RootElement);
        }

        // ----------------------------
        // List - Serialize
        // ----------------------------

        public string SerializePolymorphicList<TElement>(
            IEnumerable<TElement?>? list,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            EnsurePolymorphType<TElement>();

            return list is null
                ? "null"
                : Encoding.UTF8.GetString(SerializePolymorphicListToUtf8Bytes(options, list, wrapArrays));
        }

        public byte[] SerializePolymorphicListToUtf8Bytes<TElement>(
            IEnumerable<TElement?>? list,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            EnsurePolymorphType<TElement>();

            if (list is null)
                return Encoding.UTF8.GetBytes("null");

            var buffer = new ArrayBufferWriter<byte>(256);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                WritePolymorphicList(options, writer, list, wrapArrays);
            }

            return buffer.WrittenSpan.ToArray();
        }

        public void WritePolymorphicList<TElement>(
            Utf8JsonWriter writer,
            IEnumerable<TElement?>? list,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            Guard.NotNull(writer);
            EnsurePolymorphType<TElement>();

            if (list is null)
            {
                writer.WriteNullValue();
                return;
            }

            var el = SerializePolymorphicListToElement(options, list, wrapArrays);
            el.WriteTo(writer);
        }

        public JsonElement SerializePolymorphicListToElement<TElement>(
            IEnumerable<TElement?>? list,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            EnsurePolymorphType<TElement>();

            if (list is null)
            {
                using var doc = JsonDocument.Parse("null");
                return doc.RootElement.Clone();
            }

            var materialized = list as List<TElement?> ?? list.ToList();

            object root = wrapArrays
                ? new ListStubWithArrays<TElement> { Value = materialized }
                : new ListStub<TElement> { Value = materialized };

            var el = JsonSerializer.SerializeToElement(root, root.GetType(), options);

            if (!el.TryGetProperty(ValueName, out var vEl))
                throw new JsonException($"Internal envelope property '{ValueName}' missing.");

            return vEl.Clone();
        }

        // ----------------------------
        // List - Deserialize
        // ----------------------------

        public List<object?>? DeserializePolymorphicList(string json)
            => DeserializePolymorphicList<object?>(options, json);

        public List<TElement?>? DeserializePolymorphicList<TElement>(string json)
        {
            Guard.NotNull(options);
            Guard.NotNull(json);
            EnsurePolymorphType<TElement>();

            if (json.Length <= 8 && json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            using var doc = JsonDocument.Parse(json);
            return ReadPolymorphicListFromRootElement<TElement>(options, doc.RootElement);
        }

        public List<object?>? ReadPolymorphicList(ref Utf8JsonReader reader)
            => ReadPolymorphicList<object?>(options, ref reader);

        public List<TElement?>? ReadPolymorphicList<TElement>(ref Utf8JsonReader reader)
        {
            Guard.NotNull(options);
            EnsurePolymorphType<TElement>();

            if (reader.TokenType == JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            return ReadPolymorphicListFromRootElement<TElement>(options, doc.RootElement);
        }
    }

    private static IDictionary<string, TValue?>? ReadPolymorphicDictionaryFromRootElement<TValue>(
        JsonSerializerOptions options,
        JsonElement rootElement)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WritePropertyName(ValueName);
            rootElement.WriteTo(writer);
            writer.WriteEndObject();
        }

        // Reading does not require WrapArrays=true stub; wrapped/raw arrays are both accepted.
        var info = options.GetTypeInfo(typeof(DictionaryStub<TValue>));
        var root = JsonSerializer.Deserialize<DictionaryStub<TValue>>(buffer.WrittenSpan, options);
        return root?.Value;
    }

    private static List<TElement?>? ReadPolymorphicListFromRootElement<TElement>(
        JsonSerializerOptions options,
        JsonElement rootElement)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WritePropertyName(ValueName);
            rootElement.WriteTo(writer);
            writer.WriteEndObject();
        }

        // Reading does not require WrapArrays=true stub; wrapped/raw arrays are both accepted.
        var root = JsonSerializer.Deserialize<ListStub<TElement>>(buffer.WrittenSpan, options);
        return root?.Value;
    }

    private static void EnsurePolymorphType<T>()
    {
        var t = typeof(T);
        if (!PolymorphyCodec.IsPolymorphType(t))
        {
            throw new NotSupportedException(
                $"Type '{t}' is not a supported polymorphic type. " +
                $"Use 'object', an interface, or an abstract base type.");
        }
    }
}