using Smartstore.Core.Catalog.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute extensions
    /// </summary>
    public static class CheckoutAttributeExtensions
    {
        /// <summary>
        /// Checks whether this checkout attribute should have values
        /// </summary>
        public static bool ShouldHaveValues(this CheckoutAttribute attribute)
        {
            return attribute is not null
                && attribute.AttributeControlType is not AttributeControlType.TextBox 
                or AttributeControlType.MultilineTextbox
                or AttributeControlType.Datepicker
                or AttributeControlType.FileUpload;
        }

        /// <summary>
        /// Removes attributes from list which require shippable products
        /// </summary>
        public static IEnumerable<CheckoutAttribute> RemoveShippableAttributes(this IEnumerable<CheckoutAttribute> attributes)
        {
            Guard.NotNull(attributes, nameof(attributes));

            return attributes.Where(x => !x.ShippableProductRequired).ToList();
        }

        /// <summary>
        /// Adds an attribute value xml element
        /// </summary>
        public static string AddAttribute(this CheckoutAttribute attribute, string value, string attributes = "")
        {
            Guard.NotNull(attribute, nameof(attribute));
            Guard.NotNull(value, nameof(value));

            // TODO: (core) Build a convenient helper type for attributesXml (e.g. "ProductAttributeSelection")

            var result = string.Empty;
            try
            {
                var xmlDoc = new XmlDocument();
                if (!attributes.HasValue())
                {
                    xmlDoc.AppendChild(xmlDoc.CreateElement("Attributes"));
                }
                else
                {
                    xmlDoc.LoadXml(attributes);
                }

                var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//Attributes");

                // Find existing
                XmlElement xmlAttribute = null;
                var nodeList = xmlDoc.SelectNodes(@"//Attributes/CheckoutAttribute");
                foreach (XmlNode node in nodeList)
                {
                    if (node.Attributes is null || node.Attributes["ID"] is null)
                        continue;

                    var str = node.Attributes["ID"].InnerText.Trim();
                    if (int.TryParse(str, out var id) && id == attribute.Id)
                    {
                        xmlAttribute = (XmlElement)node;
                        break;
                    }
                }

                // Create a new one if none was found
                if (xmlAttribute is null)
                {
                    xmlAttribute = xmlDoc.CreateElement("CheckoutAttribute");
                    xmlAttribute.SetAttribute("ID", attribute.Id.ToString());
                    rootElement.AppendChild(xmlAttribute);
                }

                var xmlAttributeValue = xmlDoc.CreateElement("CheckoutAttributeValue");
                xmlAttribute.AppendChild(xmlAttributeValue);

                var xmlValue = xmlDoc.CreateElement("Value");
                xmlValue.InnerText = value;
                xmlAttributeValue.AppendChild(xmlValue);

                result = xmlDoc.OuterXml;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }

            return result;
        }
    }
}