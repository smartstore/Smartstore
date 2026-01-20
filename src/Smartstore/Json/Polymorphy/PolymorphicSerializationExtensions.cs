#nullable enable

using System.Buffers;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Polymorphy;

public static class PolymorphicSerializationExtensions
{
    public delegate object? ReadByRefDelegate(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

    private readonly struct ConverterCacheKey : IEquatable<ConverterCacheKey>
    {
        public ConverterCacheKey(Type declaredType, bool wrapArrays)
        {
            DeclaredType = declaredType;
            WrapArrays = wrapArrays;
        }

        public Type DeclaredType { get; }
        public bool WrapArrays { get; }

        public bool Equals(ConverterCacheKey other)
            => DeclaredType == other.DeclaredType && WrapArrays == other.WrapArrays;

        public override bool Equals(object? obj)
            => obj is ConverterCacheKey other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(DeclaredType, WrapArrays);
    }

    private readonly struct ConverterEntry
    {
        public required JsonConverter Converter { get; init; }
        public required Action<Utf8JsonWriter, object?, JsonSerializerOptions> Write { get; init; }
        public required ReadByRefDelegate Read { get; init; }
    }

    private static readonly ConditionalWeakTable<JsonSerializerOptions, ConcurrentDictionary<ConverterCacheKey, ConverterEntry>> _converterCache = [];

    extension(JsonSerializerOptions options)
    {
        #region Main API

        public void SerializePolymorphic(
            Utf8JsonWriter writer,
            object? value,
            Type declaredType,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            Guard.NotNull(writer);
            Guard.NotNull(declaredType);

            // Avoid null->value-type cast failures in the compiled delegate.
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var entry = GetConverterEntry(options, declaredType, wrapArrays);
            entry.Write(writer, value, options);
        }

        public object? DeserializePolymorphic(ref Utf8JsonReader reader, Type declaredType)
        {
            Guard.NotNull(options);
            Guard.NotNull(declaredType);

            var entry = GetConverterEntry(options, declaredType, false);
            return entry.Read(ref reader, declaredType, options);
        }

        #endregion

        #region Convenience overloads (STJ-shaped, but explicitly Polymorphic)

        public void SerializePolymorphic<TValue>(
            Utf8JsonWriter writer,
            TValue? value,
            bool wrapArrays = false)
            => SerializePolymorphic(options, writer, value, typeof(TValue), wrapArrays);

        public TValue? DeserializePolymorphic<TValue>(ref Utf8JsonReader reader)
            => (TValue?)DeserializePolymorphic(options, ref reader, typeof(TValue));

        public byte[] SerializePolymorphicToUtf8Bytes(
            object? value,
            Type inputType,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            Guard.NotNull(inputType);

            var buffer = new ArrayBufferWriter<byte>(256);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                SerializePolymorphic(options, writer, value, inputType, wrapArrays);
            }

            return buffer.WrittenSpan.ToArray();
        }

        public byte[] SerializePolymorphicToUtf8Bytes<TValue>(
            TValue? value,
            bool wrapArrays = false)
            => SerializePolymorphicToUtf8Bytes(options, value, typeof(TValue), wrapArrays);

        public string SerializePolymorphicToString(
            object? value,
            Type inputType,
            bool wrapArrays = false)
            => Encoding.UTF8.GetString(SerializePolymorphicToUtf8Bytes(options, value, inputType, wrapArrays));

        public string SerializePolymorphicToString<TValue>(
            TValue? value,
            bool wrapArrays = false)
            => SerializePolymorphicToString(options, value, typeof(TValue), wrapArrays);

        public JsonElement SerializePolymorphicToElement(
            object? value,
            Type inputType,
            bool wrapArrays = false)
        {
            Guard.NotNull(options);
            Guard.NotNull(inputType);

            var utf8 = SerializePolymorphicToUtf8Bytes(options, value, inputType, wrapArrays);

            using var doc = JsonDocument.Parse(utf8);
            return doc.RootElement.Clone();
        }

        public JsonElement SerializePolymorphicToElement<TValue>(
            TValue? value,
            bool wrapArrays = false)
            => SerializePolymorphicToElement(options, value, typeof(TValue), wrapArrays);

        public object? DeserializePolymorphic(ReadOnlySpan<byte> utf8Json, Type returnType)
        {
            Guard.NotNull(options);
            Guard.NotNull(returnType);

            var reader = new Utf8JsonReader(utf8Json);
            if (!reader.Read())
                return null;

            return DeserializePolymorphic(options, ref reader, returnType);
        }

        public TValue? DeserializePolymorphic<TValue>(ReadOnlySpan<byte> utf8Json)
            => (TValue?)DeserializePolymorphic(options, utf8Json, typeof(TValue));

        public object? DeserializePolymorphic(string json, Type returnType)
        {
            Guard.NotNull(options);
            Guard.NotNull(json);
            Guard.NotNull(returnType);

            if (json.Length <= 8 && json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            return DeserializePolymorphic(options, Encoding.UTF8.GetBytes(json), returnType);
        }

        public TValue? DeserializePolymorphic<TValue>(string json)
            => (TValue?)DeserializePolymorphic(options, json, typeof(TValue));

        public object? DeserializePolymorphic(JsonElement element, Type returnType)
        {
            Guard.NotNull(options);
            Guard.NotNull(returnType);

            var buffer = new ArrayBufferWriter<byte>(256);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                element.WriteTo(writer);
            }

            return DeserializePolymorphic(options, buffer.WrittenSpan, returnType);
        }

        public TValue? DeserializePolymorphic<TValue>(JsonElement element)
            => (TValue?)DeserializePolymorphic(options, element, typeof(TValue));

        #endregion
    }

    private static ConverterEntry GetConverterEntry(JsonSerializerOptions options, Type declaredType, bool wrapArrays)
    {
        var dict = _converterCache.GetValue(options, static _ => new ConcurrentDictionary<ConverterCacheKey, ConverterEntry>());
        var key = new ConverterCacheKey(declaredType, wrapArrays);

        return dict.GetOrAdd(key, k => CreateEntry(options, k.DeclaredType, k.WrapArrays));
    }

    private static ConverterEntry CreateEntry(JsonSerializerOptions options, Type declaredType, bool wrapArrays)
    {
        var kind = PolymorphyModifier.Classify(declaredType);
        var factory = PolymorphyModifier.ResolveConverterFactory(kind, wrapArrays);

        // Create the closed converter for declaredType
        var converter = factory.CreateConverter(declaredType, options)!;

        // Build fast delegates for JsonConverter<T>.Write/Read
        var write = CreateWriteDelegate(converter, declaredType);
        var read = CreateReadDelegate(converter, declaredType);

        return new ConverterEntry
        {
            Converter = converter,
            Write = write,
            Read = read
        };
    }

    private static Action<Utf8JsonWriter, object?, JsonSerializerOptions> CreateWriteDelegate(JsonConverter converter, Type declaredType)
    {
        // (Utf8JsonWriter w, object? v, JsonSerializerOptions o) => v is null ? w.WriteNullValue() : ((JsonConverter<T>)converter).Write(w, (T)v, o)
        var convType = typeof(JsonConverter<>).MakeGenericType(declaredType);

        var writerParam = Expression.Parameter(typeof(Utf8JsonWriter), "writer");
        var valueParam = Expression.Parameter(typeof(object), "value");
        var optionsParam = Expression.Parameter(typeof(JsonSerializerOptions), "options");

        var convConst = Expression.Constant(converter, convType);

        var writeMi = convType.GetMethod(
            "Write",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: [typeof(Utf8JsonWriter), declaredType, typeof(JsonSerializerOptions)],
            modifiers: null)!;

        var nullConst = Expression.Constant(null, typeof(object));
        var isNull = Expression.Equal(valueParam, nullConst);

        var writeNullMi = typeof(Utf8JsonWriter).GetMethod(
            nameof(Utf8JsonWriter.WriteNullValue),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null)!;

        var writeNullCall = Expression.Call(writerParam, writeNullMi);

        var valueCast = Expression.Convert(valueParam, declaredType);
        var writeCall = Expression.Call(convConst, writeMi, writerParam, valueCast, optionsParam);

        var body = Expression.IfThenElse(isNull, writeNullCall, writeCall);

        return Expression.Lambda<Action<Utf8JsonWriter, object?, JsonSerializerOptions>>(body, writerParam, valueParam, optionsParam)
            .Compile();
    }

    private static ReadByRefDelegate CreateReadDelegate(JsonConverter converter, Type declaredType)
    {
        // (ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => (object?)((JsonConverter<T>)converter).Read(ref r, t, o)
        var convType = typeof(JsonConverter<>).MakeGenericType(declaredType);

        var readerParam = Expression.Parameter(typeof(Utf8JsonReader).MakeByRefType(), "reader");
        var typeParam = Expression.Parameter(typeof(Type), "typeToConvert");
        var optionsParam = Expression.Parameter(typeof(JsonSerializerOptions), "options");

        var convConst = Expression.Constant(converter, convType);

        var readMi = convType.GetMethod(
            "Read",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: [typeof(Utf8JsonReader).MakeByRefType(), typeof(Type), typeof(JsonSerializerOptions)],
            modifiers: null)!;

        var call = Expression.Call(convConst, readMi, readerParam, typeParam, optionsParam);
        var box = Expression.Convert(call, typeof(object));

        return Expression.Lambda<ReadByRefDelegate>(box, readerParam, typeParam, optionsParam)
            .Compile();
    }
}