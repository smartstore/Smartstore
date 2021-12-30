using Newtonsoft.Json;

namespace Smartstore.AmazonPay.Models
{
    public class IpnMessage
    {
        [JsonProperty("MerchantID")]
        public string MerchantId { get; set; }

        public string ObjectType { get; set; }
        public string ObjectId { get; set; }
        public string ChargePemissionId { get; set; }
        public string NotificationType { get; set; }
        public string NotificationId { get; set; }
        public string NotificationVersion { get; set; }
    }
}
