using Microsoft.AspNetCore.Authentication;

namespace Smartstore.AmazonPay
{
    public class AmazonPaySignInOptions : AuthenticationSchemeOptions
    {
        public int StoreId { get; set; }
        public string BuyerToken { get; set; }

        public override void Validate()
        {
            if (BuyerToken.IsEmpty())
            {
                throw new ArgumentException("Missing buyer token for sign-in with Amazon.");
            }

            base.Validate();
        }
    }
}
