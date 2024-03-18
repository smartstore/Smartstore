namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Setting for newsletter subscription in checkout
    /// </summary>
    public enum CheckoutNewsletterSubscription
    {
        /// <summary>
        /// No newsletter subscription checkbox
        /// </summary>
        None = 0,

        /// <summary>
        /// Deactivated newsletter subscription checkbox
        /// </summary>
        Deactivated,

        /// <summary>
        /// Activated newsletter subscription checkbox
        /// </summary>
        Activated
    }

    /// <summary>
    /// Setting to hand over customer email to third party
    /// </summary>
    public enum CheckoutThirdPartyEmailHandOver
    {
        /// <summary>
        /// No third party email hand over checkbox
        /// </summary>
        None = 0,

        /// <summary>
        /// Deactivated third party email hand over checkbox
        /// </summary>
        Deactivated,

        /// <summary>
        /// Activated third party email hand over checkbox
        /// </summary>
        Activated
    }
}