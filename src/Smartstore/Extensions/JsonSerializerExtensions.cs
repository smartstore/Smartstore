#nullable enable

using Newtonsoft.Json;

namespace Smartstore
{
    public static class JsonSerializerExtensions
    {
        /// <summary>
        /// Serializes a dictionary object to JSON using the specified <see cref="JsonSerializer"/> and <see cref="JsonWriter"/>.
        /// This method sets <see cref="JsonSerializer.TypeNameHandling"/> to <see cref="TypeNameHandling.All"/>
        /// and restores the original state after serialization.
        /// </summary>
        /// <param name="serializer">The <see cref="JsonSerializer"/> instance.</param>
        /// <param name="writer">The <see cref="JsonWriter"/> instance.</param>
        /// <param name="dictionary">The dictionary object to serialize.</param>
        public static void SerializeObjectDictionary(
            this JsonSerializer serializer,
            JsonWriter writer,
            IDictionary<string, object?> dictionary)
        {
            Guard.NotNull(serializer);
            Guard.NotNull(writer);

            var typeNameHandling = serializer.TypeNameHandling;
            try
            {
                serializer.TypeNameHandling = TypeNameHandling.All;
                serializer.Serialize(writer, dictionary);
            }
            finally
            {
                serializer.TypeNameHandling = typeNameHandling;
            }
        }
    }
}
