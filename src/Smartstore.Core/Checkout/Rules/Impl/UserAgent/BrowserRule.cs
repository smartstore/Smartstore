using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class BrowserRule : IRule<CartRuleContext>
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
                "Edge",
                "Firefox",
                "Internet Explorer",
                "Safari",
                "Opera",
                "Brave",
                "Netscape",
                "Mozilla",
                "Konqueror",
                "Ubuntu Web Browser"
            }
            .Select(x => new RuleValueSelectListOption { Value = x, Text = x })
            .ToArray();
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = _userAgent.IsBrowser() && expression.HasListMatch(_userAgent.Name.NullEmpty());
            return Task.FromResult(match);
        }
    }
}
