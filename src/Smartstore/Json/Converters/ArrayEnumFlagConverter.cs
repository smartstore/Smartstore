#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Converters;

public abstract class ArrayEnumFlagConverter<TEnum> : JsonConverterFactory
    where TEnum : struct, Enum
{
    protected abstract IReadOnlyDictionary<string, TEnum> GetMapping();

    public override bool CanConvert(Type typeToConvert)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        return underlying == typeof(TEnum);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var mapping = GetMapping();
        var reverse = BuildReverse(mapping);

        var underlying = Nullable.GetUnderlyingType(typeToConvert);
        return underlying is null
            ? new EnumConverter(mapping, reverse)
            : new NullableEnumConverter(mapping, reverse);
    }


    private sealed class EnumConverter : JsonConverter<TEnum>
    {
        private readonly IReadOnlyDictionary<string, TEnum> _mapping;
        private readonly IReadOnlyList<KeyValuePair<TEnum, string>> _reverse;

        public EnumConverter(
            IReadOnlyDictionary<string, TEnum> mapping,
            IReadOnlyList<KeyValuePair<TEnum, string>> reverse)
        {
            _mapping = mapping;
            _reverse = reverse;
        }

        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ReadFlags(ref reader, _mapping);

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            => WriteFlags(writer, value, _reverse);
    }

    private sealed class NullableEnumConverter : JsonConverter<TEnum?>
    {
        private readonly IReadOnlyDictionary<string, TEnum> _mapping;
        private readonly IReadOnlyList<KeyValuePair<TEnum, string>> _reverse;

        public NullableEnumConverter(
            IReadOnlyDictionary<string, TEnum> mapping,
            IReadOnlyList<KeyValuePair<TEnum, string>> reverse)
        {
            _mapping = mapping;
            _reverse = reverse;
        }

        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            return ReadFlags(ref reader, _mapping);
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            WriteFlags(writer, value.Value, _reverse);
        }
    }

    private static TEnum ReadFlags(ref Utf8JsonReader reader, IReadOnlyDictionary<string, TEnum> mapping)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (str is not null && mapping.TryGetValue(str, out var singleFlag))
                return singleFlag;

            return default;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
            return default;

        ulong flags = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (s is not null && mapping.TryGetValue(s, out var flag))
                    flags |= Convert.ToUInt64(flag);

                continue;
            }

            // Ignore nested values safely.
            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                reader.Skip();
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), flags);
    }

    private static void WriteFlags(Utf8JsonWriter writer, TEnum value, IReadOnlyList<KeyValuePair<TEnum, string>> reverse)
    {
        var raw = Convert.ToUInt64(value);

        // "No flags" -> []
        writer.WriteStartArray();

        if (raw != 0)
        {
            foreach (var kv in reverse)
            {
                var flagRaw = Convert.ToUInt64(kv.Key);
                if (flagRaw == 0)
                    continue;

                if ((raw & flagRaw) == flagRaw)
                    writer.WriteStringValue(kv.Value);
            }
        }

        writer.WriteEndArray();
    }

    private static IReadOnlyList<KeyValuePair<TEnum, string>> BuildReverse(IReadOnlyDictionary<string, TEnum> mapping)
    {
        var list = new List<KeyValuePair<TEnum, string>>(mapping.Count);
        foreach (var kv in mapping)
            list.Add(new KeyValuePair<TEnum, string>(kv.Value, kv.Key));

        // Stable output order by numeric flag value
        list.Sort(static (a, b) => Convert.ToUInt64(a.Key).CompareTo(Convert.ToUInt64(b.Key)));
        return list;
    }
}
