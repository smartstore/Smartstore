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
            return new[]
            {
                "BlackBerry",
                "Generic Feature Phone",
                "Generic Smartphone",
                "Generic Tablet",
                "HP TouchPad",
                "iPad",
                "iPhone",
                "iPod",
                "Kindle",
                "Kindle Fire",
                "Lumia",
                "Mac",
                "Microsoft Surface RT",
                "Motorola",
                "Nokia",
                "Palm",
                "Samsung",
                "Spider",
                "Other"
            }
            .Select(x => new RuleValueSelectListOption { Value = x, Text = x })
            .ToArray();
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = expression.HasListMatch(_userAgent.Device.Family.NullEmpty());
            return Task.FromResult(match);
        }
    }
}
