namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Represents the tax rate resolution result.
    /// </summary>
    public readonly struct TaxRate
    {
        public readonly static TaxRate Zero;

        public TaxRate(decimal rate, int taxCategoryId)
        {
            Rate = rate;
            TaxCategoryId = taxCategoryId;
        }

        /// <summary>
        /// The tax rate
        /// </summary>
        public decimal Rate { get; }

        /// <summary>
        /// The tax category id
        /// </summary>
        public int TaxCategoryId { get; }

        public static implicit operator decimal(TaxRate rate) => rate.Rate;
        public static implicit operator double(TaxRate rate) => Convert.ToDouble(rate.Rate);
        public static implicit operator float(TaxRate rate) => Convert.ToSingle(rate.Rate);
        public static implicit operator int(TaxRate rate) => Convert.ToInt32(rate.Rate);
    }
}