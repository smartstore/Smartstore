using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

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
        /// Calls <see cref="GetRemainingAmountAsync(GiftCard)"/>
        /// </remarks>
        /// <returns>
        /// <c>True</c> if gift card is valid, otherwise <c>false</c>
        /// </returns>
        Task<bool> ValidateGiftCardAsync(GiftCard giftCard, int storeId = 0);

        /// <summary>
        /// Gets the gift cards remaining amount in primary currency.
        /// </summary>
        /// <remarks>
        /// Accesses <see cref="GiftCard.GiftCardUsageHistory"/>. 
        /// The caller is responsible for eager loading
        /// </remarks>
        /// <returns>
        /// Remaining usable gift card amount, including the current store's primary currency.
        /// </returns>
        Task<Money> GetRemainingAmountAsync(GiftCard giftCard);

        /// <summary>
        /// Generates a new gift card code.
        /// </summary>
        string GenerateGiftCardCode();
    }
}