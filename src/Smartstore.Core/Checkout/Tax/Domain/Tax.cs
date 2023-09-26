using System.Globalization;
using System.Runtime.CompilerServices;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Represents tax info.
    /// </summary>
    public readonly struct Tax
    {
        public readonly static Tax Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tax"/> structure.
        /// </summary>
        /// <param name="rate">The tax rate.</param>
        /// <param name="amount">The calculated tax amount.</param>
        /// <param name="price">The origin price.</param>
        /// <param name="priceNet">The calculated bet price.</param>
        /// <param name="priceGross">The calculated gross price.</param>
        /// <param name="isGrossPrice">A value indicating whether <paramref name="price"/> includes tax already.</param>
        /// <param name="inclusive">A value indicating whether the result price should be gross (including tax).</param>
        public Tax(TaxRate rate, decimal amount, decimal price, decimal priceNet, decimal priceGross, bool isGrossPrice, bool inclusive)
        {
            Rate = rate;
            Amount = amount;
            IsGrossPrice = isGrossPrice;
            Inclusive = inclusive;
            PriceNet = priceNet;
            PriceGross = priceGross;
            Price = price;
        }

        /// <summary>
        /// The tax rate used for calculation.
        /// </summary>
        public TaxRate Rate { get; }

        /// <summary>
        /// The unrounded tax amount.
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        /// Whether source price is gross (including tax)
        /// </summary>
        public bool IsGrossPrice { get; }

        /// <summary>
        /// Whether result price is gross (including tax)
        /// </summary>
        public bool Inclusive { get; }

        /// <summary>
        /// The unrounded net price.
        /// </summary>
        public decimal PriceNet { get; }

        /// <summary>
        /// The unrounded gross price.
        /// </summary>
        public decimal PriceGross { get; }

        /// <summary>
        /// The rounded price, either net or gross according to <see cref="Inclusive"/>.
        /// </summary>
        public decimal Price { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => Math.Round(Amount, 2).ToString("0.00", CultureInfo.CurrentCulture);

        public static implicit operator bool(Tax tax) => tax.Amount != 0; // For truthy checks in templating
        public static implicit operator string(Tax tax) => tax.ToString();
        public static implicit operator decimal(Tax tax) => tax.Amount;
        public static implicit operator double(Tax tax) => Convert.ToDouble(tax.Amount);
        public static implicit operator float(Tax tax) => Convert.ToSingle(tax.Amount);
        public static implicit operator int(Tax tax) => Convert.ToInt32(tax.Amount);
    }
}
