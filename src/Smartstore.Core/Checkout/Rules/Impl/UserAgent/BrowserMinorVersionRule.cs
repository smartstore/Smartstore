using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class BrowserMinorVersionRule : IRule
    {
        private readonly IUserAgent _userAgent;

        public BrowserMinorVersionRule(IUserAgent userAgent)
        {
            _userAgent = userAgent;
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = false;

            if (_userAgent.UserAgent.Minor.HasValue() && int.TryParse(_userAgent.UserAgent.Minor, out var minorVersion))
            {
                match = expression.Operator.Match(minorVersion, expression.Value);
            }

            return Task.FromResult(match);
        }
    }
}
