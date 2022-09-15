using Smartstore.Web.Modelling;

namespace Smartstore.Web.Api.Models
{
    [LocalizedDisplay("Admin.Customers.Customers.Fields.")]
    public class WebApiUserModel : EntityModelBase
    {
        [LocalizedDisplay("*Active")]
        public bool Active { get; set; }

        [LocalizedDisplay("*Username")]
        public string Username { get; set; }

        [LocalizedDisplay("*FullName")]
        public string FullName { get; set; }

        [LocalizedDisplay("*Email")]
        public string Email { get; set; }

        [LocalizedDisplay("*AdminComment")]
        public string AdminComment { get; set; }

        [LocalizedDisplay("Plugins.Api.WebApi.ApiAccess")]
        public string PublicKey { get; set; }
        public string SecretKey { get; set; }
        
        public bool Enabled { get; set; }
        public string EnabledString { get; set; }

        public DateTime? LastRequestDate { get; set; }
        public string LastRequestDateString { get; set; }

        public string EditUrl { get; set; }

        public bool CanEnable => PublicKey.HasValue() && !Enabled;
        public bool CanDisable => Enabled;
        public string ButtonDisplayRemoveKeys => PublicKey.HasValue() ? "inline-block" : "none";
        public string ButtonDisplayCreateKeys => !PublicKey.HasValue() ? "inline-block" : "none";
    }
}
