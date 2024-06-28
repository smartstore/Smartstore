using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class BrowserMinorVersionRule(IUserAgent userAgent) : IRule<CartRuleContext>
    {
        private readonly IUserAgent _userAgent = userAgent;

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = false;

            if (_userAgent.Version is Version version)
            {
                match = expression.Operator.Match(version.Minor, expression.Value);
            }

            return Task.FromResult(match);
        }
    }
}
