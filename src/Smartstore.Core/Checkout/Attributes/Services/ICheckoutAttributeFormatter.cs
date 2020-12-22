using Smartstore.Core.Customers;
using System.Threading.Tasks;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute format helper
    /// </summary>
    public partial interface ICheckoutAttributeFormatter
    {
        // TODO: (Core) (ms) needs download & tax service + price formatter

        /// <summary>
        /// Formats attributes
        /// </summary>
        //Task<string> FormatAttributesAsync(string attributes);

        /// <summary>
        /// Formats attributes
        /// </summary>
        //Task<string> FormatAttributesAsync(
        //    string attributes, 
        //    Customer customer,
        //    string serapator = "<br />",
        //    bool htmlEncode = true,
        //    bool renderPrices = true,
        //    bool allowHyperlinks = true);
    }
}