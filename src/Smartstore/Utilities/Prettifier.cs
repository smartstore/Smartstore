using System.Globalization;
using System.Text;
using System.Xml;
using Humanizer;
using Newtonsoft.Json.Linq;

namespace Smartstore.Utilities
{
    public static class Prettifier
    {
        public static string HumanizeBytes(long bytes)
        {
            string format = "#,#";

            if (bytes >= Math.Pow(1024, 3)) // > GB
            {
                format = "#,#.##";
            }
            else if (bytes >= Math.Pow(1024, 2)) // MB
            {
                format = "#,#.#";
            }

            return bytes.Bytes().Humanize(format);
        }

        public static string HumanizeTimeSpan(TimeSpan timeSpan, CultureInfo culture = null)
        {
            Guard.NotNull(timeSpan);

            return timeSpan.Humanize(timeSpan.TotalMinutes < 1 ? 1 : 2, culture ?? CultureInfo.InvariantCulture);
        }

        public static string PrettifyXML(string xml)
        {
            if (xml.IsEmpty() || xml.IsWhiteSpace())
            {
                return xml;
            }

            // first read the xml ignoring whitespace
            using (var xmlReader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { IgnoreWhitespace = true, CheckCharacters = false }))
            {
                // then write it out with indentation
                var sb = new StringBuilder(xml.Length * 2);
                using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, IndentChars = "\t", CheckCharacters = false }))
                {
                    writer.WriteNode(xmlReader, true);
                }

                var result = sb.ToString();
                return result;
            }
        }

        public static string PrettifyJSON(string json)
        {
            if (json.IsEmpty())
            {
                return json;
            }

            try
            {
                return json.StartsWith('[')
                    ? JArray.Parse(json).ToString()
                    : JToken.Parse(json).ToString();
            }
            catch
            {
                return json;
            }
        }
    }
}