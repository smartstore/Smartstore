using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class IsMobileRule : IRule<CartRuleContext>
    {
        private readonly IUserAgent _userAgent;

        public IsMobileRule(IUserAgent userAgent)
        {
            _userAgent = userAgent;
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = expression.Operator.Match(_userAgent.IsMobileDevice(), expression.Value);
            return Task.FromResult(match);
        }
    }
}
