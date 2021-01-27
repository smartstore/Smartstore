using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Smartstore.ComponentModel.TypeConverters;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Encapsulates data of a product bundle item.
    /// </summary>
    public partial class ProductBundleItemData
    {
        public ProductBundleItemData(ProductBundleItem item)
        {
            Item = item;
        }

        /// <summary>
        /// Gets the bundle item. Can be <c>null</c>.
        /// </summary>
        public ProductBundleItem Item { get; private set; }
        public decimal AdditionalCharge { get; set; }
    }

    /// <summary>
    /// Contains the deserialised bundle item data of an ordered product.
    /// </summary>
    public partial class ProductBundleItemOrderData : IAttributeAware
    {
        private ProductVariantAttributeSelection _attributeSelection;
        private string _rawAttributes;

        public int BundleItemId { get; set; }
        public int ProductId { get; set; }
        public string Sku { get; set; }
        public string ProductName { get; set; }
        public string ProductSeName { get; set; }
        public bool VisibleIndividually { get; set; }
        public int Quantity { get; set; }
        public decimal PriceWithDiscount { get; set; }
        public int DisplayOrder { get; set; }
        public string AttributesInfo { get; set; }
        public bool PerItemShoppingCart { get; set; }
        public string RawAttributes
        {
            get => _rawAttributes;
            set
            {
                _rawAttributes = value;
                _attributeSelection = null;
            }
        }

        [NotMapped]
        public ProductVariantAttributeSelection AttributeSelection
            => _attributeSelection ??= new(RawAttributes);
    }

    public class ProductBundleItemOrderDataConverter : DefaultTypeConverter
    {
        private readonly bool _forList;

        public ProductBundleItemOrderDataConverter(bool forList)
            : base(typeof(object))
        {
            _forList = forList;
        }

        public override bool CanConvertFrom(Type type)
        {
            return type == typeof(string);
        }

        public override bool CanConvertTo(Type type)
        {
            return type == typeof(string);
        }

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is not string)
            {
                return base.ConvertFrom(culture, value);
            }
            
            object result = null;
            string str = value as string;

            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    using var reader = new StringReader(str);
                    var serializer = new XmlSerializer(_forList ? typeof(List<ProductBundleItemOrderData>) : typeof(ProductBundleItemOrderData));
                    result = serializer.Deserialize(reader);
                }
                catch
                {
                }
            }

            return result;
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            if (to != typeof(string))
            {
                return base.ConvertTo(culture, format, value, to);
            }

            if (value != null && (value is ProductBundleItemOrderData || value is IList<ProductBundleItemOrderData>))
            {
                var sb = new StringBuilder();
                using var writer = new StringWriter(sb);
                var serializer = new XmlSerializer(_forList ? typeof(List<ProductBundleItemOrderData>) : typeof(ProductBundleItemOrderData));
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
