using System;
using System.Diagnostics;
using System.Xml;

namespace Smartstore.Core.Catalog.Attributes
{
    public static class ProductAttributeExtensions
    {
        public static bool ShouldHaveValues(this ProductVariantAttribute productVariantAttribute)
        {
            if (productVariantAttribute == null)
                return false;

            if (productVariantAttribute.AttributeControlType == AttributeControlType.TextBox ||
                productVariantAttribute.AttributeControlType == AttributeControlType.MultilineTextbox ||
                productVariantAttribute.AttributeControlType == AttributeControlType.Datepicker ||
                productVariantAttribute.AttributeControlType == AttributeControlType.FileUpload)
                return false;

            // All other attribute control types support values.
            return true;
        }

        public static string AddProductAttribute(this ProductVariantAttribute pva, string attributes, string value)
        {
            string result = string.Empty;
            try
            {
                var xmlDoc = new XmlDocument();
                if (!attributes.HasValue())
                {
                    var element1 = xmlDoc.CreateElement("Attributes");
                    xmlDoc.AppendChild(element1);
                }
                else
                {
                    xmlDoc.LoadXml(attributes);
                }
                var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//Attributes");

                XmlElement pvaElement = null;
                //find existing
                var nodeList1 = xmlDoc.SelectNodes(@"//Attributes/ProductVariantAttribute");
                foreach (XmlNode node1 in nodeList1)
                {
                    if (node1.Attributes != null && node1.Attributes["ID"] != null)
                    {
                        string str1 = node1.Attributes["ID"].InnerText.Trim();
                        int id = 0;
                        if (int.TryParse(str1, out id))
                        {
                            if (id == pva.Id)
                            {
                                pvaElement = (XmlElement)node1;
                                break;
                            }
                        }
                    }
                }

                //create new one if not found
                if (pvaElement == null)
                {
                    pvaElement = xmlDoc.CreateElement("ProductVariantAttribute");
                    pvaElement.SetAttribute("ID", pva.Id.ToString());
                    rootElement.AppendChild(pvaElement);
                }

                var pvavElement = xmlDoc.CreateElement("ProductVariantAttributeValue");
                pvaElement.AppendChild(pvavElement);

                var pvavVElement = xmlDoc.CreateElement("Value");
                pvavVElement.InnerText = value;
                pvavElement.AppendChild(pvavVElement);

                result = xmlDoc.OuterXml;
            }
            catch (Exception exc)
            {
                Debug.Write(exc.ToString());
            }
            return result;
        }
    }
}
