namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Represents the tax display type.
    /// </summary>
    public enum TaxDisplayType
    {
        /// <summary>
        /// Including tax.
        /// </summary>
        IncludingTax = 0,

        /// <summary>
        /// Excluding tax.
        /// </summary>
        ExcludingTax = 10
    }

    /// <summary>
    /// Represents the tax base.
    /// </summary>
    public enum TaxBasedOn
    {
        /// <summary>
        /// Billing address.
        /// </summary>
        BillingAddress = 1,

        /// <summary>
        /// Shipping address.
        /// </summary>
        ShippingAddress = 2,

        /// <summary>
        /// Default address.
        /// </summary>
        DefaultAddress = 3
    }

    /// <summary>
    /// Represents the VAT number status.
    /// </summary>
    public enum VatNumberStatus
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Empty.
        /// </summary>
        Empty = 10,

        /// <summary>
        /// Valid.
        /// </summary>
        Valid = 20,

        /// <summary>
        /// Invalid.
        /// </summary>
        Invalid = 30
    }

    /// <summary>
    /// Specifies how to calculate the tax of auxiliary services like shipping and payment fees.
    /// </summary>
    public enum AuxiliaryServicesTaxType
    {
        /// <summary>
        /// Calculate tax of auxiliary services with the tax category specified in settings.
        /// </summary>
        SpecifiedTaxCategory = 0,

        /// <summary>
        /// Calculate tax with the tax rate that has the highest amount in the cart.
        /// </summary>
        HighestCartAmount = 10,

        /// <summary>
        /// Calculate tax by the highest tax rate in the cart.
        /// </summary>
        HighestTaxRate = 15
    }
}
