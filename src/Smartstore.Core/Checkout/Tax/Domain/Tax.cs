using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Represents tax info.
    /// </summary>
    public readonly struct Tax
    {
        // TODO: (core) Move Money, Tax & ICurrency to Smartstore.Financial later.
        public readonly static Tax Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tax"/> structure.
        /// </summary>
        /// <param name="rate">The tax rate</param>
        /// <param name="amount">The calculated tax amount</param>
        /// <param name="price">The origin price</param>
        /// <param name="inclusive">Whether <paramref name="price"/> includes tax already</param>
        public Tax(decimal rate, decimal amount, decimal price, bool inclusive)
        {
            Rate = rate;
            Amount = amount;
            Inclusive = inclusive;
            PriceNet = inclusive ? price - amount : price;
            PriceGross = inclusive ? price : price + amount;
        }

        public decimal Rate { get; }
        public decimal Amount { get; }
        public bool Inclusive { get; }
        public decimal PriceNet { get; }
        public decimal PriceGross { get; }

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
