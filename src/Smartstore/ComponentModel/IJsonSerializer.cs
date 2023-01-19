#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Smartstore.ComponentModel
{
    /// <summary>
    /// Responsible for converting objects from and to JSON.
    /// </summary>
    public interface IJsonSerializer
    {
        bool CanSerialize(object? obj);
        bool CanSerialize(Type? objectType);
        bool CanDeserialize(Type? objectType);

        bool TrySerialize(object? value, bool compress, [MaybeNullWhen(false)] out byte[]? result);
        bool TryDeserialize(Type objectType, byte[] value, bool uncompress, [MaybeNullWhen(false)] out object? result);
    }
}