namespace Smartstore.AmazonPay.Models
{
    [Serializable]
    public class AmazonPayOrderAttribute
    {
        public string OrderReferenceId { get; set; }
        public bool OrderReferenceClosed { get; set; }
    }
}
