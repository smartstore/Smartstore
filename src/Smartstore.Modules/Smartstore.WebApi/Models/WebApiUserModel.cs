using Smartstore.Web.Modelling;

namespace Smartstore.WebApi.Models
{
    [LocalizedDisplay("Admin.Customers.Customers.Fields.")]
    public class WebApiUserModel : EntityModelBase
    {
        [LocalizedDisplay("*Username")]
        public string Username { get; set; }

        [LocalizedDisplay("*Email")]
        public string Email { get; set; }

        [LocalizedDisplay("*AdminComment")]
        public string AdminComment { get; set; }

        public string PublicKey { get; set; }
        public string SecretKey { get; set; }
        public bool Enabled { get; set; }
        public string EnabledFriendly { get; set; }
        public DateTime? LastRequestDate { get; set; }
        public string LastRequestDateString { get; set; }
    }
}
