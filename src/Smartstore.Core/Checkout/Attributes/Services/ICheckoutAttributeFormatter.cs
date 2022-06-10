using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout Attributes formatting helper.
    /// </summary>
    public partial interface ICheckoutAttributeFormatter
    {
        /// <summary>
        /// Formats <see cref="CheckoutAttribute"/> and <see cref="CheckoutAttributeValue"/>.
        /// </summary>
        /// <param name="selection">Checkout attribute selection</param>
        /// <param name="customer">Is <see cref="IWorkContext.CurrentCustomer"/>, if <c>null</c></param>
        /// <param name="serapator">String to seperate each attribute</param>
        /// <param name="htmlEncode">Indicates wheter to HTML encode the string</param>
        /// <param name="renderPrices">Indicates whether to render prices</param>
        /// <param name="allowHyperlinks">Indicates whether to display hyperlinks</param>
        /// <returns>
        /// Formated <see cref="CheckoutAttributeSelection"/> as string.
        /// </returns>
        Task<string> FormatAttributesAsync(
            CheckoutAttributeSelection selection,
            Customer customer = null,
            string serapator = "<br />",
            bool htmlEncode = true,
            bool renderPrices = true,
            bool allowHyperlinks = true);
    }
}