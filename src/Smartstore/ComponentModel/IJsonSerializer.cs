namespace Smartstore.ComponentModel
{
    /// <summary>
    /// Responsible for converting objects from and to JSON.
    /// </summary>
    public interface IJsonSerializer
    {
        bool CanSerialize(object obj);
        bool CanSerialize(Type objectType);
        bool CanDeserialize(Type objectType);

        bool TrySerialize(object value, bool compress, out byte[] result);
        bool TryDeserialize(Type objectType, byte[] value, bool uncompress, out object result);
    }
}