using System.IO;
using System.Xml.Serialization;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Utilities;

namespace Smartstore.AmazonPay
{
    internal static class OrderExtensions
    {
        public static void SetAmazonPayOrderReference(this Order order, AmazonPayOrderReference orderReference)
        {
            Guard.NotNull(order, nameof(order));
            Guard.NotNull(orderReference, nameof(orderReference));

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            using var writer = new StringWriter(sb);

            var serializer = new XmlSerializer(typeof(AmazonPayOrderReference));
            serializer.Serialize(writer, orderReference);

            order.GenericAttributes.Set(AmazonPayProvider.SystemName + ".OrderAttribute", sb.ToString(), order.StoreId);
        }

        public static AmazonPayOrderReference GetAmazonPayOrderReference(this Order order)
        {
            Guard.NotNull(order, nameof(order));

            var rawAttribute = order.GenericAttributes.Get<string>(AmazonPayProvider.SystemName + ".OrderAttribute", order.StoreId);

            if (!rawAttribute.HasValue())
            {
                // Legacy < v.1.14
                var orderReference = new AmazonPayOrderReference
                {
                    OrderReferenceId = order.GenericAttributes.Get<string>(AmazonPayProvider.SystemName + ".OrderReferenceId", order.StoreId)
                };

                return orderReference;
            }

            using var reader = new StringReader(rawAttribute);
            var serializer = new XmlSerializer(typeof(AmazonPayOrderReference));

            return (AmazonPayOrderReference)serializer.Deserialize(reader);
        }


    }
}
