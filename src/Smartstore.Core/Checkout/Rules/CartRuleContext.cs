using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Rules
{
    public class CartRuleContext
    {
        private readonly Func<object> _sessionKeyBuilder;
        private object _sessionKey;

        internal CartRuleContext(Func<object> sessionKeyBuilder)
        {
            _sessionKeyBuilder = sessionKeyBuilder;
        }

        public Customer Customer { get; init; }
        public Store Store { get; init; }
        public IWorkContext WorkContext { get; init; }

        public object SessionKey => _sessionKey ??= _sessionKeyBuilder?.Invoke() ?? 0;
    }
}
