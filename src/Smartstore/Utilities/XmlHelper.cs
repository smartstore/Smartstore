using System.Xml.Serialization;

namespace Smartstore.Utilities
{
    /// <summary>
    /// Helper to serialize\deserialize XML.
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// Serializes an object instance to a XML formatted string.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="instance">Object instance.</param>
        /// <returns>XML string.</returns>
        public static string Serialize<T>(T instance)
        {
            return Serialize(instance, typeof(T));
        }

        /// <summary>
        /// Serializes an object instance to a XML formatted string.
        /// </summary>
        /// <param name="instance">Object instance.</param>
        /// <param name="type">Object type.</param>
        /// <returns>XML string.</returns>
        public static string Serialize(object instance, Type type)
        {
            if (instance == null)
                return null;

            Guard.NotNull(type, nameof(type));

            using var writer = new StringWriter();
            var xmlSerializer = new XmlSerializer(type);

            xmlSerializer.Serialize(writer, instance);
            return writer.ToString();
        }

        /// <summary>
        /// Deserializes a XML formatted string to an object instance.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="xml">XML string.</param>
        /// <returns>Object instance.</returns>
        public static T Deserialize<T>(string xml)
        {
            return (T)Deserialize(xml, typeof(T));
        }

        /// <summary>
        /// Deserializes a XML formatted string to an object instance.
        /// </summary>
        /// <param name="xml">XML string.</param>
        /// <param name="type">Object type.</param>
        /// <returns>Object instance.</returns>
        public static object Deserialize(string xml, Type type)
        {
            Guard.NotNull(type, nameof(type));

            try
            {
                if (xml.HasValue())
                {
                    using var reader = new StringReader(xml);
                    var serializer = new XmlSerializer(type);
                    return serializer.Deserialize(reader);
                }
            }
            catch { }

            return Activator.CreateInstance(type);
        }
    }
}
