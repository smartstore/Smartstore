using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Web.Modelling;

namespace SmartStore.DellyManLogistics.Models
{
    [LocalizedDisplay("Plugins.Smartstore.DellyManLogistics.")]
    public class ConfigurationModel : ModelBase
    {
        //[LocalizedDisplay("*PublicKey")]
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerId { get; set; }
        public string CompanyId { get; set; }
        public string PickupRequestedTime { get; set; }
        public string OrderTrackingUrl { get; set; }
        public decimal DefaultDeliveryFee { get; set; }
        public string DefaultPickUpContactName { get; set; }
        public string DefaultPickUpContactNumber { get; set; }
        public string DefaultPickUpGoogleAddress { get; set; }
        public string DefaultPickUpCity { get; set; }
        public string DefaultPickUpState { get; set; }
    }
}
