using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
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
        [JsonPropertyName("id")]
        public int ShippingMethodId { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        [JsonPropertyName("order")]
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the system name of shipping rate computation method
        /// </summary>
        [JsonPropertyName("systemName")]
        public string ShippingRateComputationMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets a shipping rate (without discounts, additional shipping charges, etc)
        /// </summary>
        [JsonPropertyName("rate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public decimal Rate { get; set; }

        /// <summary>
        /// Gets or sets a shipping option name
        /// </summary>
        [JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a shipping option description
        /// </summary>
        [JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Description { get; set; }
    }

    public class ShippingOptionConverterProvider : ITypeConverterProvider
    {
        // Keep a cached converter for sequence scenarios (common path).
        private static readonly ITypeConverter DefaultListConverter = new ShippingOptionConverter(forList: true);

        public ITypeConverter GetConverter(Type type)
        {
            if (type == typeof(ShippingOption))
            {
                // This converter's behavior depends solely on forList; safe to reuse.
                return ShippingOptionConverter.Single;
            }

            if (!type.IsArray && type.IsEnumerableType(out var elementType) && elementType == typeof(ShippingOption))
            {
                return DefaultListConverter;
            }

            return null;
        }
    }

    public class ShippingOptionConverter : DefaultTypeConverter
    {
        // XmlSerializer creation is expensive; cache per target type.
        private static readonly XmlSerializer SerializerSingle = new(typeof(ShippingOption));
        private static readonly XmlSerializer SerializerList = new(typeof(List<ShippingOption>));

        // Cache converter instances (they are stateless aside from the bool flag).
        internal static readonly ShippingOptionConverter Single = new(forList: false);
        internal static readonly ShippingOptionConverter List = new(forList: true);

        private readonly bool _forList;

        public ShippingOptionConverter(bool forList)
            : base(typeof(object))
        {
            _forList = forList;
        }

        private XmlSerializer Serializer => _forList ? SerializerList : SerializerSingle;

        public override bool CanConvertFrom(Type type) => type == typeof(string);

        public override bool CanConvertTo(Type type) => type == typeof(string);

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is not string str)
                return base.ConvertFrom(culture, value);

            // Avoid HasValue() (likely alloc/extra checks). This is the only thing we need here.
            if (string.IsNullOrWhiteSpace(str))
                return null;

            try
            {
                using var reader = new StringReader(str);
                return Serializer.Deserialize(reader);
            }
            catch
            {
                // xml error
                return null;
            }
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            if (to != typeof(string))
                return base.ConvertTo(culture, format, value, to);

            if (value is null)
                return string.Empty;

            // Keep original semantics: only serialize ShippingOption or list.
            // (The list case is accepted via IList<ShippingOption>.)
            if (value is not (ShippingOption or IList<ShippingOption>))
                return string.Empty;

            // Avoid intermediate StringBuilder/StringWriter allocations where possible.
            // Serialize directly into a StringWriter (backed by StringBuilder) but reuse cached serializer.
            var sb = new StringBuilder(256);
            using var writer = new StringWriter(sb, CultureInfo.InvariantCulture);
            Serializer.Serialize(writer, value);
            return sb.ToString();
        }
    }
}