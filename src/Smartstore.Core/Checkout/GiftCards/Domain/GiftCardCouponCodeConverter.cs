using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Gift card coupon code converter
    /// </summary>
    public class GiftCardCouponCodeConverter : DefaultTypeConverter
    {
        public GiftCardCouponCodeConverter()
            : base(typeof(object))
        {
        }

        public override bool CanConvertFrom(Type type)
            => type == typeof(string);

        public override bool CanConvertTo(Type type)
            => type == typeof(string);

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is string str)
            {
                object result = null;
                if (str.HasValue())
                {
                    try
                    {
                        // Convert either from XML or JSON
                        // Check first letter to determine format
                        var firstChar = str.Trim()[0];
                        if (firstChar is '<')
                        {
                            // It's XML
                            var attributes = new List<string>();
                            var xElement = XElement.Parse(str);
                            var elements = xElement.Descendants("CouponCode");

                            foreach (var element in elements)
                            {
                                var code = element.Attribute("Code")?.Value ?? null;
                                if (code.HasValue())
                                {
                                    attributes.Add(code);
                                }
                            }

                            return attributes;
                        }
                        else if (firstChar is '{' or '[')
                        {
                            // It's JSON
                            return JsonConvert.DeserializeObject<List<string>>(str);
                        }
                    }
                    catch (JsonSerializationException ex)
                    {
                        throw new JsonSerializationException("Error while trying to deserialize object from Json: " + str, ex);
                    }
                    catch (XmlException ex)
                    {
                        throw new XmlException("Error while trying to parse from XML: " + str, ex);
                    }
                    catch
                    {
                    }
                }

                return result;
            }

            return base.ConvertFrom(culture, value);
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            if (to != typeof(string))
            {
                return base.ConvertTo(culture, format, value, to);
            }

            if (value is not null and IList<string> attributes)
            {
                // XML
                //var root = new XElement("Attributes");
                //var attributeElement = new XElement("GiftCardCouponCodes");
                //foreach (var attribute in attributes)
                //{
                //    var valueElement = new XElement("CouponCode", new XAttribute("Code", attribute));
                //    attributeElement.Add(valueElement);
                //}

                //root.Add(attributeElement);

                //return root.ToString(SaveOptions.DisableFormatting);

                // JSON
                return JsonConvert.SerializeObject(attributes);
            }
            else
            {
                return string.Empty;
            }
        }
    }
}