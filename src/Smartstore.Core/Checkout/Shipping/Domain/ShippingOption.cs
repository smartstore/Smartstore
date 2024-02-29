using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.Core.Checkout.Shipping
{
    /// <summary>
    /// Represents a shipping option
    /// </summary>
    public partial class ShippingOption
    {
        /// <summary>
        /// Shipping method identifier
        /// </summary>
        [JsonProperty("id")]
        public int ShippingMethodId { get; set; }

        /// <summary>
        /// Gets or sets the system name of shipping rate computation method
        /// </summary>
        [JsonProperty("systemName")]
        public string ShippingRateComputationMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets a shipping rate (without discounts, additional shipping charges, etc)
        /// </summary>
        [JsonProperty("rate", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(typeof(decimal), "0.0")]
        public decimal Rate { get; set; }

        /// <summary>
        /// Gets or sets a shipping option name
        /// </summary>
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a shipping option description
        /// </summary>
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Description { get; set; }
    }

    public class ShippingOptionConverterProvider : ITypeConverterProvider
    {
        static readonly ITypeConverter Default = new ShippingOptionConverter(true);

        public ITypeConverter GetConverter(Type type)
        {
            if (type == typeof(ShippingOption))
            {
                return new ShippingOptionConverter(false);
            }
            else if (!type.IsArray && type.IsEnumerableType(out var elementType) && elementType == typeof(ShippingOption))
            {
                return Default;
            }

            return null;
        }
    }

    public class ShippingOptionConverter : DefaultTypeConverter
    {
        private readonly bool _forList;

        public ShippingOptionConverter(bool forList)
            : base(typeof(object))
        {
            _forList = forList;
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
                        using var reader = new StringReader(str);
                        var serializer = new XmlSerializer(_forList ? typeof(List<ShippingOption>) : typeof(ShippingOption));
                        result = serializer.Deserialize(reader);
                    }
                    catch
                    {
                        // xml error
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

            if (value is not null and (ShippingOption or IList<ShippingOption>))
            {
                var sb = new StringBuilder(100);
                using var writer = new StringWriter(sb);
                var serializer = new XmlSerializer(_forList ? typeof(List<ShippingOption>) : typeof(ShippingOption));
                serializer.Serialize(writer, value);
                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
    }
}