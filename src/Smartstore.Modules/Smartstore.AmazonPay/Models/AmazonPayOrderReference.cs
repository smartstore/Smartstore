using System.Xml.Serialization;

namespace Smartstore.AmazonPay.Models
{
    /// <summary>
    /// Amazon Pay order reference which is XML serialized and saved as generic attribute value.
    /// It is required to later void a payment and to close the order reference at Amazon Pay.
    /// </summary>
    [Serializable]
    [XmlType("AmazonPayOrderAttribute")]
    public class AmazonPayOrderReference
    {
        public string OrderReferenceId { get; set; }
        public bool OrderReferenceClosed { get; set; }
    }
}
