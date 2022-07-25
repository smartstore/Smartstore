using System.Globalization;
using System.Xml;
using Newtonsoft.Json;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Import
{
    public static partial class ImportUtility
    {
        private readonly static XmlReaderSettings _xmlValidatorSettings = new()
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
            CheckCharacters = true
        };

        /// <summary>
        /// Converts a raw import value and returns <c>null</c> for a value of zero.
        /// </summary>
        /// <param name="value">Import value.</param>
        /// <param name="culture">Culture info.</param>
        public static int? ZeroToNull(object value, CultureInfo culture)
        {
            if (ConvertUtility.TryConvert(value, culture, out int result) && result > 0)
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// A simple XML validation by reading the first XML nodes.
        /// </summary>
        /// <param name="xml">Raw XML.</param>
        /// <returns><c>true</c> XML is valid otherwise <c>false</c>.</returns>
        public static bool ValidateXml(string xml, XmlReaderSettings settings = null)
        {
            try
            {
                using (var stringReader = new StringReader(xml))
                using (var xmlReader = XmlReader.Create(stringReader, settings ?? _xmlValidatorSettings))
                {
                    var i = 0;
                    while (xmlReader.Read() && i < 3)
                    {
                        ++i;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// A simple JSON validation by reading the first JSON tokens.
        /// </summary>
        /// <param name="json">Raw JSON.</param>
        /// <returns><c>true</c> JSON is valid otherwise <c>false</c>.</returns>
        public static bool ValidateJson(string json)
        {
            try
            {
                using (var stringReader = new StringReader(json))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var i = 0;
                    while (jsonReader.Read() && i < 5)
                    {
                        ++i;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// A simple validation of either XML or JSON formatted string.
        /// </summary>
        /// <param name="xmlOrJson">XML or JSON formatted string.</param>
        /// <returns><c>true</c> valid XML ot JSON otherwise <c>false</c>.</returns>
        public static bool ValidateXmlOrJson(ref string xmlOrJson)
        {
            if (xmlOrJson != null && xmlOrJson.Length > 0)
            {
                xmlOrJson = xmlOrJson.TrimStart();

                if (xmlOrJson[0] == '<')
                {
                    return ValidateXml(xmlOrJson);
                }
                else if (xmlOrJson[0] == '{')
                {
                    return ValidateJson(xmlOrJson);
                }
            }

            return false;
        }
    }
}
