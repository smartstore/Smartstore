using System.Runtime.CompilerServices;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;

namespace Smartstore
{
    public static partial class IRoundingHelperExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Round(this IRoundingHelper service, decimal amount, RoundingReason reason, Currency currency = null)
        {
            Guard.NotNull(service);

            return service.Round(amount, reason, false, currency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal RoundTax(this IRoundingHelper service, decimal amount, RoundingReason reason, Currency currency = null)
        {
            Guard.NotNull(service);

            return service.Round(amount, reason, true, currency);
        }

        /// <summary>
        /// Round value up or down to the nearest multiple of denomination (cash rounding) if activated for currency.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="toNearestRounding">Amount by which was rounded.</param>
        /// <param name="currency">
        /// Currency. Rounding must be activated for this currency.
        /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <returns>Rounded amount.</returns>
        /// <example>"Schweizer Rappenrundung" of 16.23 -> returned value is 16.25 and toNearestRounding is 0.02.</example>
        /// <remarks>Usually this method is used to round the order total.</remarks>
        public static Money RoundToNearest(this IRoundingHelper service, Money amount, out Money toNearestRounding, Currency currency = null)
        {
            Guard.NotNull(service);

            var newValue = service.RoundToNearest(amount.Amount, out var tmpToNearestRounding, currency);

            toNearestRounding = new(tmpToNearestRounding, currency);

            return new(newValue, currency);
        }
    }
}
