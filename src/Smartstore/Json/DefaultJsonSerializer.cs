#nullable enable

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.ComponentModel;
using Smartstore.Json.Polymorphy;

namespace Smartstore.Json;

public class DefaultJsonSerializer : IJsonSerializer
{
    private readonly struct PolymorphyKindCacheEntry(bool isSlot, PolymorphyKind kind, Type? elementType)
    {
        public bool IsSlot { get; } = isSlot;
        public PolymorphyKind Kind { get; } = kind;
        public Type? ElementType { get; } = elementType;
    }

    private static readonly byte[] NullResult = "null"u8.ToArray();

    private readonly ConcurrentDictionary<Type, PolymorphyKindCacheEntry> _polymorphyCache = new();
    private readonly ConcurrentDictionary<Type, byte> _nonWritableTypes = new();
    private readonly ConcurrentDictionary<Type, byte> _nonReadableTypes = new();
    private readonly ConcurrentDictionary<Type, Type?> _genericTypeDefinitionCache = new();

    public DefaultJsonSerializer()
    {
        SeedUnserializableDefaults();
    }

    protected virtual JsonSerializerOptions CreateDefaultOptions()
        => SmartJsonOptions.Default;

    protected JsonSerializerOptions Options => field ??= CreateDefaultOptions();

    public ILogger Logger { get; set; } = NullLogger.Instance;

    private void SeedUnserializableDefaults()
    {
        _nonWritableTypes.TryAdd(typeof(Task), 0);
        _nonWritableTypes.TryAdd(typeof(Task<>), 0);

        _nonReadableTypes.TryAdd(typeof(Task), 0);
        _nonReadableTypes.TryAdd(typeof(Task<>), 0);
    }

    public bool CanSerialize(object? obj)
        => CanSerialize(GetInnerType(obj));

    public virtual bool CanSerialize(Type? objectType)
        => IsSerializableType(objectType, _nonWritableTypes);

    public virtual bool CanDeserialize(Type? objectType)
        => IsSerializableType(objectType, _nonReadableTypes);

    public bool TrySerialize(object? value, bool compress, [MaybeNullWhen(false)] out byte[]? result)
    {
        result = null;

        if (!CanSerialize(value))
            return false;

        try
        {
            result = SerializeCore(value, compress);
            return true;
        }
        catch (NotSupportedException ex)
        {
            var t = GetInnerType(value);
            if (t != null)
            {
                _nonWritableTypes.TryAdd(t, 0);
                Logger.Debug(ex, "Type '{Type}' cannot be serialized (NotSupported).", t);
            }

            return false;
        }
        catch (Exception ex)
        {
            var t = GetInnerType(value);
            if (t != null)
                Logger.Debug(ex, "Serialization failed for type '{Type}'.", t);

            return false;
        }
    }

    public bool TryDeserialize(Type objectType, byte[] value, bool uncompress, [MaybeNullWhen(false)] out object? result)
    {
        Guard.NotNull(objectType);
        Guard.NotNull(value);

        result = null;

        if (!CanDeserialize(objectType))
            return false;

        try
        {
            result = DeserializeCore(objectType, value, uncompress);
            return true;
        }
        catch (NotSupportedException ex)
        {
            if (ShouldBlacklistDeserializeType(objectType))
            {
                _nonReadableTypes.TryAdd(objectType, 0);
                Logger.Debug(ex, "Type '{Type}' cannot be DEserialized (NotSupported).", objectType);
            }

            return false;
        }
        catch (JsonException ex)
        {
            Logger.Debug(ex, "Deserialization failed for type '{Type}' due to invalid JSON payload.", objectType);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Deserialization failed for type '{Type}'.", objectType);
            return false;
        }
    }

    private object? DeserializeCore(Type objectType, byte[] value, bool uncompress)
    {
        // Keep exact behavior: literal "null" means null.
        if (value.AsSpan().SequenceEqual(NullResult))
            return null;

        if (uncompress)
            value = value.Unzip();

        if (IsPolymorphicType(objectType))
        {
            return Options.DeserializePolymorphic(value, objectType);
        }

        return JsonSerializer.Deserialize(value, objectType, Options);
    }

    private byte[] SerializeCore(object? value, bool compress)
    {
        if (value is null)
            return compress ? NullResult.Zip() : NullResult;

        var runtimeType = GetInnerType(value) ?? value.GetType();

        var buffer = new ArrayBufferWriter<byte>(256);

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false
        }))
        {
            if (IsPolymorphicObject(value))
            {
                Options.SerializePolymorphic(writer, value, runtimeType, wrapArrays: true);
            }
            else
            {
                JsonSerializer.Serialize(writer, value, runtimeType, Options);
            }
        }

        var bytes = buffer.WrittenSpan.ToArray();
        return compress ? bytes.Zip() : bytes;
    }

    private bool IsSerializableType(Type? objectType, ConcurrentDictionary<Type, byte> set)
    {
        if (objectType is null)
            return true;

        if (set.TryGetValue(objectType, out _))
            return false;

        var genericDef = GetGenericTypeDefinitionCached(objectType);
        if (genericDef != null && set.TryGetValue(genericDef, out _))
            return false;

        return true;
    }

    private Type? GetGenericTypeDefinitionCached(Type type)
    {
        return _genericTypeDefinitionCache.GetOrAdd(type, static t =>
        {
            return t.IsGenericType ? t.GetGenericTypeDefinition() : null;
        });
    }

    private static Type? GetInnerType(object? obj)
    {
        if (obj is IObjectContainer wrapper)
            return wrapper.Value?.GetType();

        return obj?.GetType();
    }

    private static bool ShouldBlacklistDeserializeType(Type objectType)
    {
        return !(typeof(IObjectContainer).IsAssignableFrom(objectType)
                 || objectType == typeof(object)
                 || objectType.IsBasicOrNullableType());
    }

    private bool IsPolymorphicObject(object value)
    {
        // Fast path for common polymorphic slot types.
        if (value is IDictionary<string, object?> || value is ICollection<object?> || value is ISet<object?>)
            return true;
        
        // Slow (but cached) path
        return IsPolymorphicType(value.GetType());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsPolymorphicType(Type t)
        => TryGetPolymorphyKind(t, out var _, out var _);

    private bool TryGetPolymorphyKind(Type type, [NotNullWhen(true)] out PolymorphyKind? kind, [NotNullWhen(true)] out Type? elementType)
    {
        kind = default;
        elementType = typeof(object);

        if (type.IsBasicOrNullableType())
        {
            return false;
        }
        
        // Note: elementType out param must be non-null when returning true.
        if (_polymorphyCache.TryGetValue(type, out var entry))
        {
            if (entry.IsSlot && entry.ElementType is not null)
            {
                kind = entry.Kind;
                elementType = entry.ElementType;
                return true;
            }

            return false;
        }

        if (PolymorphyCodec.TryGetPolymorphyKind(type, out kind, out elementType))
        {
            _polymorphyCache.TryAdd(type, new PolymorphyKindCacheEntry(true, kind.Value, elementType));
            return true;
        }

        // Negative cache entry.
        _polymorphyCache.TryAdd(type, new PolymorphyKindCacheEntry(false, default, null));

        return false;
    }
}
