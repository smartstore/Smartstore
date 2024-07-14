using Smartstore.Core.Common.Services;
using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class IPCountryRule(IGeoCountryLookup countryLookup, IWebHelper webHelper) : IRule<CartRuleContext>
    {
        private readonly IGeoCountryLookup _countryLookup = countryLookup;
        private readonly IWebHelper _webHelper = webHelper;

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var country = _countryLookup.LookupCountry(_webHelper.GetClientIpAddress());
            var match = expression.HasListMatch(country?.IsoCode ?? string.Empty, StringComparer.InvariantCultureIgnoreCase);

            return Task.FromResult(match);
        }
    }
}
