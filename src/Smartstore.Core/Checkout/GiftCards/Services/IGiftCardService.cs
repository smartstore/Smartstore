using Smartstore.Core.Customers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Gift card service interface
    /// </summary>
    public partial interface IGiftCardService
    {
        /// <summary>
        /// Gets query with all gift card entries
        /// </summary>
        /// <returns>Query with all gift cards</returns>
        IQueryable<GiftCard> GetAllGiftCardsQuery();

        /// <summary>
        /// Gets active gift cards that are applied by customer async
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>List of active gift cards</returns>
        Task<IList<GiftCard>> GetActiveGiftCardsAppliedByCustomerAsync(Customer customer, int storeId);

        /// <summary>
        /// Generates new gift card code
        /// </summary>
        /// <returns>Gift card code</returns>
        string GenerateGiftCardCode();
    }
}
