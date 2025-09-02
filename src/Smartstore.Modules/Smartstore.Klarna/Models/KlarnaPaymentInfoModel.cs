using Smartstore.Web.Modelling;

namespace Smartstore.Klarna.Models
{
    public class KlarnaPaymentInfoModel : ModelBase
    {
        public string ClientToken { get; set; }
        public string PublicKey { get; set; } // If Klarna requires a public key for JS SDK
        // Add any other data needed by the Klarna JS SDK on the client-side
    }
}
