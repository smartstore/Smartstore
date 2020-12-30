using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Gift card service interface
    /// </summary>
    public partial interface IGiftCardService
    {
        //TODO: (ms) (core) customer extension ParseAppliedGiftCardCouponCodes is needed + nav props > load eager(include)
        /// <summary>
        /// Gets active gift cards that are applied by customer async
        /// </summary>
        Task<List<AppliedGiftCard>> GetAppliedGiftCardsByCustomerAsync(Customer customer, int storeId = 0);

        /// <summary>
        /// Generates new gift card code async
        /// </summary>
        Task<string> GenerateGiftCardCodeAsync();

        /// <summary>
        /// Checks whether the gift card is valid
        /// </summary>
        bool ValidateGiftCard(GiftCard giftCard, int storeId = 0);

        /// <summary>
        /// Gets the gift cards remaining value
        /// </summary>
        decimal GetRemainingAmount(GiftCard giftCard);
    }
}