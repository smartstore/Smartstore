using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class BrowserRule : IRule
    {
        private readonly IUserAgent _userAgent;

        public BrowserRule(IUserAgent userAgent)
        {
            _userAgent = userAgent;
        }

        public static RuleValueSelectListOption[] GetDefaultOptions()
        {
            return new[]
            {
                "Chrome",
                "Chrome Mobile",
                "Edge",
                "Firefox",
                "Firefox Mobile",
                "IE",
                "IE Mobile",
                "Mobile Safari",
                "Opera",
                "Opera Mobile",
                "Opera Mini",
                "Safari",
                "Samsung Internet"
            }
            .Select(x => new RuleValueSelectListOption { Value = x, Text = x })
            .ToArray();
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = expression.HasListMatch(_userAgent.UserAgent.Family.NullEmpty());
            return Task.FromResult(match);
        }
    }
}
