using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Attributes
{
    public class CheckoutAttributeSelection : AttributeSelection
    {
        public CheckoutAttributeSelection(string rawAttributes)
            : base("CheckoutAttribute", rawAttributes)
        {
        }
    }
}
