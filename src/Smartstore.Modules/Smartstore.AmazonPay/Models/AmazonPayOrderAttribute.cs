namespace Smartstore.AmazonPay.Models
{
    // TODO: (mg) (core) This class is NOT an attribute and should not end with "Attribute"
    [Serializable]
    public class AmazonPayOrderAttribute
    {
        public string OrderReferenceId { get; set; }
        public bool OrderReferenceClosed { get; set; }
    }
}
