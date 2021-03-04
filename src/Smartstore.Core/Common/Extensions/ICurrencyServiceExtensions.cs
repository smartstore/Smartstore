using System.Runtime.CompilerServices;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Common.Services
{
    public static partial class ICurrencyServiceExtensions
    {
        /// <summary>
        /// Converts a currency amount into the exchange rate of a store.
        /// </summary>
        /// <param name="currencyService">Currency service.</param>
        /// <param name="amount">Source currency and amount to be converted.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>Converted currency amount where <see cref="Money.Currency"/> is the primary exchange rate currency.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Money ConvertToPrimaryExchangeRateCurrency(this ICurrencyService currencyService, Money amount, Store store = null)
        {
            Guard.NotNull(currencyService, nameof(currencyService));

            return currencyService.ConvertToStoreCurrency(true, amount, store);
        }

        /// <summary>
        /// Converts a currency amount into the primary currency of a store.
        /// </summary>
        /// <param name="currencyService">Currency service.</param>
        /// <param name="amount">Source currency and amount to be converted.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>Converted currency amount where <see cref="Money.Currency"/> is the primary store currency.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Money ConvertToPrimaryStoreCurrency(this ICurrencyService currencyService, Money amount, Store store = null)
        {
            Guard.NotNull(currencyService, nameof(currencyService));

            return currencyService.ConvertToStoreCurrency(false, amount, store);
        }

        /// <summary>
        /// Converts a currency amount from the exchange rate currency of a store.
        /// </summary>
        /// <param name="currencyService">Currency service.</param>
        /// <param name="amount">Target currency and amount to be converted.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>Converted currency amount where <see cref="Money.Currency"/> is the currency of <paramref name="amount"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Money ConvertFromPrimaryExchangeRateCurrency(this ICurrencyService currencyService, Money amount, Store store = null)
        {
            Guard.NotNull(currencyService, nameof(currencyService));

            return currencyService.ConvertFromStoreCurrency(true, amount, store);
        }

        /// <summary>
        /// Converts a currency amount from the primary currency of a store.
        /// </summary>
        /// <param name="currencyService">Currency service.</param>
        /// <param name="amount">Target currency and amount to be converted.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>Converted currency amount where <see cref="Money.Currency"/> is the currency of <paramref name="amount"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Money ConvertFromPrimaryStoreCurrency(this ICurrencyService currencyService, Money amount, Store store = null)
        {
            Guard.NotNull(currencyService, nameof(currencyService));

            return currencyService.ConvertFromStoreCurrency(false, amount, store);
        }
    }
}
