namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Specifies how to calculate the tax of auxiliary services like shipping and payment fees
    /// </summary>
    public enum AuxiliaryServicesTaxType
    {
        /// <summary>
        /// Calculate tax of auxiliary services with the tax category specified in settings
        /// </summary>
        SpecifiedTaxCategory = 0,

        /// <summary>
        /// Calculate tax with the tax rate that has the highest amount in the cart
        /// </summary>
        HighestCartAmount = 10,

        /// <summary>
        /// Calculate tax by the highest tax rate in the cart
        /// </summary>
        HighestTaxRate = 15
    }
}