using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Smartstore.ComponentModel
{
    internal static class SerializationUtils
    {
        /// <summary>
        /// Overload that supports passing in an XML TextWriter. 
        /// </summary>
        /// <remarks>
        /// Note the Writer is not closed when serialization is complete 
        /// so the caller needs to handle closing.
        /// </remarks>
        /// <param name="instance">object to serialize</param>
        /// <param name="writer">XmlTextWriter instance to write output to</param>       
        /// <param name="throwExceptions">Determines whether false is returned on failure or an exception is thrown</param>
        /// <returns></returns>
        public static bool SerializeObject(object instance, XmlTextWriter writer, bool throwExceptions)
        {
            bool retVal = true;

            try
            {
                XmlSerializer serializer =
                    new(instance.GetType());

                // Create an XmlTextWriter using a FileStream.
                writer.Formatting = Formatting.Indented;
                writer.IndentChar = ' ';
                writer.Indentation = 3;

                // Serialize using the XmlTextWriter.
                serializer.Serialize(writer, instance);
            }
            catch (Exception ex)
            {
                Debug.Write("SerializeObject failed with : " + ex.GetBaseException().Message + Environment.NewLine + (ex.InnerException != null ? ex.InnerException.Message : ""), "West Wind");

                if (throwExceptions)
                    throw;

                retVal = false;
            }

            return retVal;
        }


        /// <summary>
        /// Serializes an object into an XML string variable for easy 'manual' serialization
        /// </summary>
        /// <param name="instance">object to serialize</param>
        /// <param name="xmlResultString">resulting XML string passed as an out parameter</param>
        /// <returns>true or false</returns>
        public static bool SerializeObject(object instance, out string xmlResultString)
        {
            return SerializeObject(instance, out xmlResultString, false);
        }

        /// <summary>
        /// Serializes an object into a string variable for easy 'manual' serialization
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="xmlResultString">Out parm that holds resulting XML string</param>
        /// <param name="throwExceptions">If true causes exceptions rather than returning false</param>
        /// <returns></returns>
        public static bool SerializeObject(object instance, out string xmlResultString, bool throwExceptions)
        {
            xmlResultString = string.Empty;
            MemoryStream ms = new MemoryStream();

            XmlTextWriter writer = new(ms, new UTF8Encoding());

            if (!SerializeObject(instance, writer, throwExceptions))
            {
                ms.Close();
                return false;
            }

            xmlResultString = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);

            ms.Close();
            writer.Close();

            return true;
        }

        /// <summary>
        /// Serializes an object to an XML string. Unlike the other SerializeObject overloads
        /// this methods *returns a string* rather than a bool result!
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="throwExceptions">Determines if a failure throws or returns null</param>
        /// <returns>
        /// null on error otherwise the Xml String.         
        /// </returns>
        /// <remarks>
        /// If null is passed in null is also returned so you might want
        /// to check for null before calling this method.
        /// </remarks>
        public static string SerializeObjectToString(object instance, bool throwExceptions = false)
        {
            if (!SerializeObject(instance, out var xmlResultString, throwExceptions))
                return null;

            return xmlResultString;
        }

        /// <summary>
        /// Deserialize an object from an XmlReader object.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public static object DeSerializeObject(XmlReader reader, Type objectType)
        {
            XmlSerializer serializer = new XmlSerializer(objectType);
            object Instance = serializer.Deserialize(reader);
            reader.Close();

            return Instance;
        }

        public static object DeSerializeObject(string xml, Type objectType)
        {
            XmlTextReader reader = new XmlTextReader(xml, XmlNodeType.Document, null);
            return DeSerializeObject(reader, objectType);
        }

        /// <summary>
        /// Returns a string of all the field value pairs of a given object.
        /// Works only on non-statics.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ObjectToString(object instance, string separator, ObjectToStringTypes type)
        {
            var fi = instance.GetType().GetFields();

            string output = string.Empty;

            if (type == ObjectToStringTypes.Properties || type == ObjectToStringTypes.PropertiesAndFields)
            {
                foreach (var property in instance.GetType().GetProperties())
                {
                    try
                    {
                        output += property.Name + ":" + property.GetValue(instance, null).ToString() + separator;
                    }
                    catch
                    {
                        output += property.Name + ": n/a" + separator;
                    }
                }
            }

            if (type == ObjectToStringTypes.Fields || type == ObjectToStringTypes.PropertiesAndFields)
            {
                foreach (var field in fi)
                {
                    try
                    {
                        output = output + field.Name + ": " + field.GetValue(instance).ToString() + separator;
                    }
                    catch
                    {
                        output = output + field.Name + ": n/a" + separator;
                    }
                }
            }
            return output;
        }

    }

    public enum ObjectToStringTypes
    {
        Properties,
        PropertiesAndFields,
        Fields
    }
}