using Smartstore.Core.Common;

namespace Smartstore
{
    public static class CurrencyExtensions
    {
        /// <summary>
        /// Checks if a currency was configured for the domain ending.
        /// </summary>
        /// <param name="domain">Domain to check.</param>
        public static bool HasDomainEnding(this Currency currency, string domain)
        {
            if (currency == null || domain.IsEmpty() || currency.DomainEndings.IsEmpty())
                return false;

            var endings = currency.DomainEndings.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return endings.Any(x => domain.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Returns the currency configured for the domain ending.
        /// </summary>
        /// <param name="domain">Domain to check.</param>
        public static Currency GetByDomainEnding(this IEnumerable<Currency> currencies, string domain)
        {
            if (currencies == null || domain.IsEmpty())
                return null;

            return currencies.FirstOrDefault(x => x.Published && x.HasDomainEnding(domain));
        }
    }
}
