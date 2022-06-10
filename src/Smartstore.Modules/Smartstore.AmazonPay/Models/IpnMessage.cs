namespace Smartstore.AmazonPay.Models
{
    public class IpnMessage
    {
        public string MerchantID { get; set; }
        public string ObjectType { get; set; }
        public string ObjectId { get; set; }
        public string ChargePermissionId { get; set; }
        public string NotificationType { get; set; }
        public string NotificationId { get; set; }
        public string NotificationVersion { get; set; }
    }
}
