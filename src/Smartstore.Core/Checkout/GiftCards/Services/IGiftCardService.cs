using Smartstore.Core.Customers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Gift card service interface
    /// </summary>
    public partial interface IGiftCardService
    {
        /// <summary>
        /// Gets active gift cards that are applied by customer async
        /// </summary>
        Task<IList<GiftCard>> GetActiveGiftCardsAppliedByCustomerAsync(Customer customer, int storeId);

        /// <summary>
        /// Generates new gift card code async
        /// </summary>
        Task<string> GenerateGiftCardCodeAsync();
    }
}
