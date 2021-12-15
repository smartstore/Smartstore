using System.IO;
using System.Xml.Serialization;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Utilities;

namespace Smartstore.AmazonPay
{
    internal static class OrderExtensions
    {
        public static void SetAmazonPayAttribute(this Order order, AmazonPayOrderAttribute attribute)
        {
            Guard.NotNull(order, nameof(order));
            Guard.NotNull(attribute, nameof(attribute));

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            using var writer = new StringWriter(sb);

            var serializer = new XmlSerializer(typeof(AmazonPayOrderAttribute));
            serializer.Serialize(writer, attribute);

            order.GenericAttributes.Set(AmazonPayProvider.SystemName + ".OrderAttribute", sb.ToString(), order.StoreId);
        }

        public static AmazonPayOrderAttribute GetAmazonPayAttribute(this Order order)
        {
            Guard.NotNull(order, nameof(order));

            var rawAttribute = order.GenericAttributes.Get<string>(AmazonPayProvider.SystemName + ".OrderAttribute", order.StoreId);

            if (!rawAttribute.HasValue())
            {
                // Legacy < v.1.14
                var attribute = new AmazonPayOrderAttribute
                {
                    OrderReferenceId = order.GenericAttributes.Get<string>(AmazonPayProvider.SystemName + ".OrderReferenceId", order.StoreId)
                };

                return attribute;
            }

            using var reader = new StringReader(rawAttribute);
            var serializer = new XmlSerializer(typeof(AmazonPayOrderAttribute));

            return (AmazonPayOrderAttribute)serializer.Deserialize(reader);
        }


    }
}
