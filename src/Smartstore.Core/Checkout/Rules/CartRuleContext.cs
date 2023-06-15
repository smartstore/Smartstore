using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Rules
{
    public class CartRuleContext
    {
        private readonly Func<object> _sessionKeyBuilder;
        private object _sessionKey;
        private ShoppingCart _cart;

        internal CartRuleContext(Func<object> sessionKeyBuilder)
        {
            _sessionKeyBuilder = sessionKeyBuilder;
        }

        public IWorkContext WorkContext { get; init; }
        public IShoppingCartService ShoppingCartService { get; init; }

        public Customer Customer { get; set; }
        public Store Store { get; set; }

        public ShoppingCart ShoppingCart
        {
            get => _cart ??= ShoppingCartService.GetCartAsync(Customer, storeId: Store.Id).Await();
            set => _cart = value;
        }

        public object SessionKey 
            => _sessionKey ??= _sessionKeyBuilder?.Invoke() ?? 0;
    }
}
