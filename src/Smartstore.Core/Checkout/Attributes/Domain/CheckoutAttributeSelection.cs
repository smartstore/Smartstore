using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <inheritdoc/>
    public class CheckoutAttributeSelection : AttributeSelection
    {
        /// <inheritdoc cref="AttributeSelection(string, string, string)"/>
        public CheckoutAttributeSelection(string attributesRaw)
            : base(attributesRaw, "CheckoutAttribute")
        {
        }
    }
}