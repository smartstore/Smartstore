#nullable enable

using System.Globalization;
using System.Xml;

namespace Smartstore;

public static class XmlNodeExtensions
{
    extension(XmlNode node)
    {
        /// <summary>
        /// Safe way to get inner text of an attribute.
        /// </summary>
        public T? GetAttributeText<T>(string? attributeName, T? defaultValue = default)
        {
            try
            {
                if (node != null && !string.IsNullOrEmpty(attributeName))
                {
                    var attr = node.Attributes?[attributeName];
                    if (attr != null)
                    {
                        return attr.InnerText.Convert<T>();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Dump();
            }

            return defaultValue;
        }

        /// <summary>
        /// Safe way to get inner text of an attribute.
        /// </summary>
        public string? GetAttributeText(string? attributeName)
        {
            return node.GetAttributeText<string>(attributeName, null);
        }

        /// <summary>
        /// Safe way to get inner text of a node.
        /// </summary>
        public T? GetText<T>(string? xpath = null, T? defaultValue = default, CultureInfo? culture = null)
        {
            try
            {
                if (node != null)
                {
                    if (string.IsNullOrEmpty(xpath))
                    {
                        return node.InnerText.Convert<T>();
                    }

                    var n = node.SelectSingleNode(xpath);

                    if (n != null && n.InnerText.HasValue())
                    {
                        return n.InnerText.Convert<T>(culture);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Dump();
            }

            return defaultValue;
        }

        /// <summary>
        /// Safe way to get inner text of a node.
        /// </summary>
        public string? GetText(string? xpath = null, string? defaultValue = default)
        {
            return node.GetText<string>(xpath, defaultValue);
        }
    }
}
