using System;
using System.Diagnostics;
using System.Xml;

namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductVariantAttributeExtensions
    {
        /// <summary>
        /// Gets a value indicating whether this variant attribute should have values.
        /// </summary>
        /// <param name="productVariantAttribute">Product variant attribute.</param>
        /// <returns>A value indicating whether this variant attribute should have values.</returns>
        public static bool ShouldHaveValues(this ProductVariantAttribute productVariantAttribute)
        {
            if (productVariantAttribute == null)
            {
                return false;
            }

            return productVariantAttribute.AttributeControlType switch
            {
                AttributeControlType.TextBox or 
                AttributeControlType.MultilineTextbox or 
                AttributeControlType.Datepicker or 
                AttributeControlType.FileUpload => false,
                _ => true,  // All other attribute control types support values.
            };
        }

        /// <summary>
        /// Adds a variant attribute to attribute XML.
        /// </summary>
        /// <param name="productVariantAttribute">Product variant attribute.</param>
        /// <param name="attributes">Attribute XML.</param>
        /// <param name="value">Attribute value.</param>
        /// <returns>Updated attribute XML.</returns>
        public static string AddProductAttribute(this ProductVariantAttribute productVariantAttribute, string attributes, string value)
        {
            // TODO: (mg) (core) No attributesXml anymore. Use "AttributeSelection" instead (TBD with MS)
            var result = string.Empty;

            try
            {
                var xmlDoc = new XmlDocument();

                if (string.IsNullOrEmpty(attributes))
                {
                    var element1 = xmlDoc.CreateElement("Attributes");
                    xmlDoc.AppendChild(element1);
                }
                else
                {
                    xmlDoc.LoadXml(attributes);
                }

                // Find existing node.
                var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//Attributes");
                var nodeList = xmlDoc.SelectNodes(@"//Attributes/ProductVariantAttribute");
                XmlElement pvaElement = null;

                foreach (XmlNode node in nodeList)
                {
                    if (node.Attributes != null && node.Attributes["ID"] != null)
                    {
                        var str = node.Attributes["ID"].InnerText.Trim();

                        if (int.TryParse(str, out var id))
                        {
                            if (id == productVariantAttribute.Id)
                            {
                                pvaElement = (XmlElement)node;
                                break;
                            }
                        }
                    }
                }

                // Create new element if not found.
                if (pvaElement == null)
                {
                    pvaElement = xmlDoc.CreateElement("ProductVariantAttribute");
                    pvaElement.SetAttribute("ID", productVariantAttribute.Id.ToString());
                    rootElement.AppendChild(pvaElement);
                }

                var pvavElement = xmlDoc.CreateElement("ProductVariantAttributeValue");
                pvaElement.AppendChild(pvavElement);

                var pvavVElement = xmlDoc.CreateElement("Value");
                pvavVElement.InnerText = value;
                pvavElement.AppendChild(pvavVElement);

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
