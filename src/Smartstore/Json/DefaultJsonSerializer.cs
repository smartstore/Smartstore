#nullable enable

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.ComponentModel;

namespace Smartstore.Json;

public class DefaultJsonSerializer : IJsonSerializer
{
    private static readonly byte[] NullResult = "null"u8.ToArray();

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

    public bool TrySerialize(object? value, Type inputType, bool compress, [MaybeNullWhen(false)] out byte[]? result)
    {
        result = null;

        if (!CanSerialize(value))
            return false;

        try
        {
            result = SerializeCore(value, inputType, compress);
            return true;
        }
        catch (NotSupportedException ex)
        {
            var t = inputType ?? GetInnerType(value);
            if (t != null)
            {
                _nonWritableTypes.TryAdd(t, 0);
                Logger.Debug(ex, "Type '{Type}' cannot be serialized (NotSupported).", t);
            }

            return false;
        }
        catch (Exception ex)
        {
            var t = inputType ?? GetInnerType(value);
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

        return JsonSerializer.Deserialize(value, objectType, Options);
    }

    private byte[] SerializeCore(object? value, Type inputType, bool compress)
    {
        if (value is null)
            return compress ? NullResult.Zip() : NullResult;

        var runtimeType = inputType ?? value.GetType();

        var buffer = new ArrayBufferWriter<byte>(256);

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false
        }))
        {
            JsonSerializer.Serialize(writer, value, runtimeType, Options);
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
}
