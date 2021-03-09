using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Identity;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Gift card service interface
    /// </summary>
    public partial interface IGiftCardService
    {
        /// <summary>
        /// Gets valid gift cards as <see cref="AppliedGiftCard"/>.
        /// Valid gift cards are activated, do have a remaining usable amount and belong to the indicated store
        /// </summary>
        /// <param name="customer">Gets gift cards applied by customer</param>
        /// <remarks>
        /// If customer is not null, coupon codes applied by the customer are used as additional filters
        /// </remarks>
        /// <returns>
        /// Valid applied gift cards (by customer if not null) with remaining usable amount
        /// </returns>
        Task<List<AppliedGiftCard>> GetValidGiftCardsAsync(int storeId = 0, Customer customer = null);

        /// <summary>
        /// Checks whether the gift card is valid
        /// </summary>
        /// <remarks>
        /// Access <see cref="GiftCard.PurchasedWithOrderItem"/>, <see cref="OrderItem.Order"/> navigation properties. 
        /// The caller is responsible for eager loading.
        /// Calls <see cref="GetRemainingAmount(GiftCard)"/>
        /// </remarks>
        /// <returns>
        /// <c>True</c> if gift card is valid; Otherwise <c>false</c>
        /// </returns>
        bool ValidateGiftCard(GiftCard giftCard, int storeId = 0);

        /// <summary>
        /// Gets the gift cards remaining value
        /// </summary>
        /// <remarks>
        /// Accesses <see cref="GiftCard.GiftCardUsageHistory"/>. 
        /// The caller is responsible for eager loading
        /// </remarks>
        /// <returns>
        /// Remaining usable gift card amount, including the current store's primary currency.
        /// </returns>
        Money GetRemainingAmount(GiftCard giftCard);

        /// <summary>
        /// Generates new gift card code async
        /// </summary>
        Task<string> GenerateGiftCardCodeAsync();
    }
}