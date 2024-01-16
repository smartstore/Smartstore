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
            var country = _countryLookup.LookupCountry(_webHelper.GetClientIpAddress());
            var match = expression.HasListMatch(country?.IsoCode ?? string.Empty, StringComparer.InvariantCultureIgnoreCase);

            return Task.FromResult(match);
        }
    }
}
