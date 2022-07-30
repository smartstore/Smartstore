using System.Xml;

namespace Smartstore
{
    public static class XmlWriterExtensions
    {
        public static void WriteCData(this XmlWriter writer, string name, string value, string prefix = null, string ns = null)
        {
            if (name.HasValue() && value != null)
            {
                if (prefix == null && ns == null)
                    writer.WriteStartElement(name);
                else
                    writer.WriteStartElement(prefix, name, ns);

                writer.WriteCData(value.RemoveInvalidXmlChars());

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Created a simple or CData node element
        /// </summary>
        /// <param name="writer">The writer</param>
        /// <param name="name">Node name</param>
        /// <param name="value">Node value</param>
        /// <param name="cultureCode">The language culture code. Always converted to lowercase!</param>
        /// <param name="asCData">Whether to create simple or CData node</param>
        public static void Write(this XmlWriter writer, string name, string value, string cultureCode = null, bool asCData = false)
        {
            if (name.HasValue() && value != null)
            {
                if (cultureCode.HasValue() && value.IsEmpty())
                {
                    // Do not create too many empty nodes for empty localized values
                    return;
                }

                writer.WriteStartElement(name);

                if (cultureCode.HasValue())
                {
                    writer.WriteAttributeString("culture", cultureCode.ToLower());
                }

                if (asCData)
                {
                    writer.WriteCData(value.RemoveInvalidXmlChars());
                }
                else
                {
                    writer.WriteString(value.RemoveInvalidXmlChars());
                }

                writer.WriteEndElement();
            }
        }
    }
}
