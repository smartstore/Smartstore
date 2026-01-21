#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Smartstore.Json;

/// <summary>
/// Defines methods for serializing and deserializing objects to and from JSON format.
/// </summary>
public interface IJsonSerializer
{
    /// <summary>
    /// Determines whether the specified object can be serialized by the serializer.
    /// </summary>
    bool CanSerialize(object? obj);

    /// <summary>
    /// Determines whether the specified type can be serialized by the serializer.
    /// </summary>
    bool CanSerialize(Type? objectType);

    /// <summary>
    /// Determines whether the specified type is supported for deserialization by the serializer.
    /// </summary>
    bool CanDeserialize(Type? objectType);

    /// <summary>
    /// Attempts to serialize the specified object to a byte array, with optional compression.
    /// </summary>
    /// <remarks>Use this method to convert objects into a binary format suitable for storage or transmission.
    /// Ensure that <paramref name="inputType"/> accurately reflects the type of <paramref name="value"/>. If
    /// compression is enabled, the output may be smaller but require additional processing to decompress.</remarks>
    /// <param name="value">The object to serialize. This value can be null, in which case serialization will fail.</param>
    /// <param name="inputType">The type of the object to serialize. Must be a valid, serializable type that matches the runtime type of
    /// <paramref name="value"/>.</param>
    /// <param name="compress">A value indicating whether to GZIP-compress the serialized output. Specify <see langword="true"/> to compress the
    /// result; otherwise, <see langword="false"/>.</param>
    /// <param name="result">When this method returns <see langword="true"/>, contains the serialized byte array; otherwise, <see
    /// langword="null"/>.</param>
    /// <returns><see langword="true"/> if the object was successfully serialized; otherwise, <see langword="false"/>.</returns>
    bool TrySerialize(object? value, Type inputType, bool compress, [MaybeNullWhen(false)] out byte[]? result);

    /// <summary>
    /// Attempts to deserialize a byte array into an object of the specified type.
    /// </summary>
    /// <param name="objectType">The type of the object to deserialize the byte array into. This must be a valid type that can be deserialized.</param>
    /// <param name="value">The byte array containing the serialized data to be deserialized. This array must not be null or empty.</param>
    /// <param name="uncompress">A boolean value indicating whether to uncompress the data before deserialization. If <see langword="true"/>, the
    /// method will attempt to decompress the byte array.</param>
    /// <param name="result">When this method returns <see langword="true"/>, contains the deserialized object; otherwise, it is <see
    /// langword="null"/>.</param>
    /// <returns>Returns <see langword="true"/> if the deserialization was successful; otherwise, <see langword="false"/>.</returns>
    bool TryDeserialize(Type objectType, byte[] value, bool uncompress, [MaybeNullWhen(false)] out object? result);
}

public static class IJsonSerializerExtensions
{
    /// <summary>
    /// Determines whether the specified type can be serialized by the given JSON serializer.
    /// </summary>
    public static bool CanSerialize<T>(this IJsonSerializer serializer)
        => serializer.CanSerialize(typeof(T));

    /// <summary>
    /// Attempts to serialize the specified object to a byte array, optionally applying compression to the output.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySerialize(this IJsonSerializer serializer, object? value, bool compress, [MaybeNullWhen(false)] out byte[]? result)
        => serializer.TrySerialize(value, value?.GetType()!, compress, out result);

    /// <summary>
    /// Serializes the specified object to a byte array using the provided JSON serializer, with an option to compress
    /// the output.
    /// </summary>
    public static byte[] Serialize<T>(this IJsonSerializer serializer, T? value, bool compress)
    {
        if (serializer.TrySerialize(value, typeof(T), compress, out var result) && result != null)
        {
            return result;
        }
        throw new InvalidOperationException($"Serialization of type '{typeof(T)}' failed.");
    }

    /// <summary>
    /// Attempts to deserialize a byte array containing JSON data into an object of the specified type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDeserialize<T>(this IJsonSerializer serializer, byte[] value, bool uncompress, [MaybeNullWhen(false)] out T? result)
        => serializer.TryDeserialize(typeof(T), value, uncompress, out var obj) && obj is T t ? (result = t) != null : (result = default) == null;

    /// <summary>
    /// Deserializes a byte array containing JSON data into an object of the specified type.
    /// </summary>
    public static T Deserialize<T>(this IJsonSerializer serializer, byte[] value, bool uncompress)
    {
        if (serializer.TryDeserialize(typeof(T), value, uncompress, out var obj) && obj is T t)
        {
            return t;
        }
        throw new InvalidOperationException($"Deserialization to type '{typeof(T)}' failed.");
    }
}