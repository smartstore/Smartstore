using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class OSRule : IRule
    {
        private readonly IUserAgent _userAgent;

        public OSRule(IUserAgent userAgent)
        {
            _userAgent = userAgent;
        }

        public static RuleValueSelectListOption[] GetDefaultOptions()
        {
            return new[]
            {
                "Android",
                "BlackBerry OS",
                "BlackBerry Tablet OS",
                "Chrome OS",
                "Firefox OS",
                "iOS",
                "Kindle",
                "Linux",
                "Mac OS X",
                "Symbian OS",
                "Ubuntu",
                "webOS",
                "Windows",
                "Windows Mobile",
                "Windows Phone",
                "Windows CE"
            }
            .Select(x => new RuleValueSelectListOption { Value = x, Text = x })
            .ToArray();
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = expression.HasListMatch(_userAgent.OS.Family.NullEmpty());
            return Task.FromResult(match);
        }
    }
}
