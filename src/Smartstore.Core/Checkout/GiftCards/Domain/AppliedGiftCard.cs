using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Represents an applied gift card.
    /// </summary>
    public partial class AppliedGiftCard
    {
        /// <summary>
        /// Gets or sets the usable amount in primary currency.
        /// </summary>
        public Money UsableAmount { get; init; }

        /// <summary>
        /// Gets or sets the gift card.
        /// </summary>
        public GiftCard GiftCard { get; init; }
    }
}
