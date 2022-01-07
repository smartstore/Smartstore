using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class BrowserMajorVersionRule : IRule
    {
        private readonly IUserAgent _userAgent;

        public BrowserMajorVersionRule(IUserAgent userAgent)
        {
            _userAgent = userAgent;
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = false;

            if (_userAgent.UserAgent.Major.HasValue() && int.TryParse(_userAgent.UserAgent.Major, out var majorVersion))
            {
                match = expression.Operator.Match(majorVersion, expression.Value);
            }

            return Task.FromResult(match);
        }
    }
}
