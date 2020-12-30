using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Attributes.Domain
{
    public class CheckoutAttributeSelection : AttributeSelection
    {
        public CheckoutAttributeSelection(string attributeName = "CheckoutAttribute", string attributeValue = "CheckoutAttributeValue")
            : base(attributeName, attributeValue)
        {
        }
    }
}
