using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Web.Modelling;

namespace Smartstore.Paystack.Models
{
    [LocalizedDisplay("Plugins.Smartstore.Paystack.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*PublicKey")]
        public string PublicKey { get; set; }

        [LocalizedDisplay("*PrivateKey")]
        public string PrivateKey { get; set; }

        [LocalizedDisplay("*BaseUrl")]
        public string BaseUrl { get; set; }

       
    
}
}
