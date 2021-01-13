namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Represents an applied gift card
    /// </summary>
    public partial class AppliedGiftCard
    {
        /// <summary>
        /// Gets or sets the usable amount
        /// </summary>
        public decimal UsableAmount { get; set; }

        /// <summary>
        /// Gets the gift card
        /// </summary>
        public GiftCard GiftCard { get; set; }
    }
}
