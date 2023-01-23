using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Configuration;

namespace Smartstore.Paystack.Configuration
{
    public class PaystackSettings: ISettings
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string BaseUrl { get; set; }
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFeePercentage { get; set; }
        public decimal Fee { get; set; }
    }
}
