using Smartstore.Domain;

namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Represents a gift card coupon code
    /// </summary>
    /// <remarks>
    /// This class can parse strings with XML or JSON format to <see cref="Multimap{TKey, TValue}"/> and vice versa.
    /// </remarks>
    public class GiftCardCoupon : AttributeSelection
    {
        /// <summary>
        /// Creates gift card coupon code from string as <see cref="Multimap{int, object}"/>. 
        /// Use <see cref="AttributeSelection.AttributesMap"/> to access parsed attributes afterwards.
        /// </summary>
        /// <remarks>
        /// Automatically differentiates between XML and JSON.
        /// </remarks>        
        /// <param name="rawAttributes">XML or JSON attributes string.</param>  
        public GiftCardCoupon(string rawAttributes)
            : base(rawAttributes, "CouponCode", "Code")
        {
        }
    }
}
