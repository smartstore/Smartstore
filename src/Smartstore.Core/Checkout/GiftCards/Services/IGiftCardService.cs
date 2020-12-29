using System.Threading.Tasks;

namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Gift card service interface
    /// </summary>
    public partial interface IGiftCardService
    {
        //TODO: (core) (ms) customer extension ParseAppliedGiftCardCouponCodes is needed + nav props > load eager(include)
        /// <summary>
        /// Gets active gift cards that are applied by customer async
        /// </summary>
        //Task<IEnumerable<GiftCard>> GetActiveGiftCardsAppliedByCustomerAsync(Customer customer, int storeId);

        /// <summary>
        /// Generates new gift card code async
        /// </summary>
        Task<string> GenerateGiftCardCodeAsync();
    }
}