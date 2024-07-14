﻿using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class BrowserMajorVersionRule(IUserAgent userAgent) : IRule<CartRuleContext>
    {
        private readonly IUserAgent _userAgent = userAgent;

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = false;

            if (_userAgent.Version is Version version && version.Major > 0)
            {
                match = expression.Operator.Match(version.Major, expression.Value);
            }

            return Task.FromResult(match);
        }
    }
}
