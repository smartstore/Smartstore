using Smartstore.Core.Rules;
using Smartstore.Core.Web;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class DeviceRule : IRule
    {
        private readonly IUserAgent _userAgent;

        public DeviceRule(IUserAgent userAgent)
        {
            _userAgent = userAgent;
        }

        public static RuleValueSelectListOption[] GetDefaultOptions()
        {
            return UserAgentPatterns.Mobiles.Values
                .Distinct()
                .OrderBy(x => x)
                .Select(x => new RuleValueSelectListOption { Value = x, Text = x })
                .ToArray();
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = _userAgent.IsMobileDevice() && expression.HasListMatch(_userAgent.Device.Name.NullEmpty());
            return Task.FromResult(match);
        }
    }
}
