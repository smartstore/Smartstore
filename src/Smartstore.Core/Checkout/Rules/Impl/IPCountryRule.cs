using System.Net;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class IPCountryRule : IRule<CartRuleContext>
    {
        private readonly IGeoCountryLookup _countryLookup;
        private readonly IWebHelper _webHelper;

        public IPCountryRule(IGeoCountryLookup countryLookup, IWebHelper webHelper)
        {
            _countryLookup = countryLookup;
            _webHelper = webHelper;
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var ipAddress = _webHelper.GetClientIpAddress();
            var countryIsoCode = ipAddress != IPAddress.None
                ? _countryLookup.LookupCountry(ipAddress)?.IsoCode?.NullEmpty()
                : null;

            countryIsoCode ??= _countryLookup.LookupCountry(context.Customer?.LastIpAddress)?.IsoCode?.NullEmpty();

            var match = expression.HasListMatch(countryIsoCode ?? string.Empty, StringComparer.InvariantCultureIgnoreCase);
            return Task.FromResult(match);
        }
    }
}
