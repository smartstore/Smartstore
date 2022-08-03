using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.Core.Checkout.GiftCards
{
    public class GiftCardCouponCodeConverterProvider : ITypeConverterProvider
    {
        static readonly ITypeConverter Default = new GiftCardCouponCodeConverter();

        public ITypeConverter GetConverter(Type type)
        {
            if (!type.IsArray && type.IsEnumerableType(out var elementType) && elementType == typeof(GiftCardCouponCode))
            {
                return Default;
            }

            return null;
        }
    }

    /// <summary>
    /// Gift card coupon code converter
    /// </summary>
    internal class GiftCardCouponCodeConverter : DefaultTypeConverter
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
                            var attributes = new List<GiftCardCouponCode>();
                            var xel = XElement.Parse(str);
                            var elements = xel.Descendants("CouponCode");

                            foreach (var element in elements)
                            {
                                var code = element.Attribute("Code")?.Value ?? null;
                                if (code.HasValue())
                                {
                                    attributes.Add(new(code));
                                }
                            }

                            return attributes;
                        }
                        else if (firstChar is '[')
                        {
                            // It's JSON
                            return JsonConvert.DeserializeObject<List<string>>(str)
                                .Select(x => new GiftCardCouponCode(x))
                                .ToList();
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

            if (value is not null and IEnumerable<GiftCardCouponCode> attributes)
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
                var converted = attributes.Select(x => x.Value);
                return JsonConvert.SerializeObject(converted);
            }
            else
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// This class is needed for <see cref="GiftCardCouponCodeConverter"/> to explicitly define converter from 
    /// <see cref="List{GiftCardCouponCode}"/> to string and vice versa.
    /// </summary>
    public class GiftCardCouponCode
    {
        public GiftCardCouponCode(string value)
        {
            Value = value;
        }

        public string Value { get; init; }

        public static explicit operator string(GiftCardCouponCode code) => code.Value;
        public static explicit operator GiftCardCouponCode(string code) => new(code);
    }
}