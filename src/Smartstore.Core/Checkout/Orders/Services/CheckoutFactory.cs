using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders.Handlers;

namespace Smartstore.Core.Checkout.Orders
{
    public class CheckoutFactory : ICheckoutFactory
    {
        private readonly IEnumerable<Lazy<ICheckoutHandler, CheckoutHandlerMetadata>> _handlers;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly bool _isStandardCheckout;

        public CheckoutFactory(
            IEnumerable<Lazy<ICheckoutHandler, CheckoutHandlerMetadata>> handlers,
            ShoppingCartSettings shoppingCartSettings)
        {
            _handlers = handlers.OrderBy(x => x.Metadata.Order);
            _shoppingCartSettings = shoppingCartSettings;

            _isStandardCheckout = shoppingCartSettings.CheckoutTemplate.EqualsNoCase(CheckoutTemplateNames.Standard);
        }

        public virtual CheckoutStep[] GetCheckoutSteps()
        {
            if (_isStandardCheckout)
            {
                return _handlers.Select(Convert).ToArray();
            }
            
            if (_shoppingCartSettings.CheckoutTemplate.EqualsNoCase(CheckoutTemplateNames.Terminal))
            {
                var confirmHandler = _handlers.FirstOrDefault(x => x.Metadata.HandlerType.Equals(typeof(ConfirmHandler)));

                return [Convert(confirmHandler)];
            }

            throw new NotSupportedException($"Unknown checkout template \"{_shoppingCartSettings.CheckoutTemplate.NaIfEmpty()}\".");
        }

        public virtual CheckoutStep GetCheckoutStep(string action, string controller = "Checkout", string area = null)
        {
            Guard.NotEmpty(action);
            Guard.NotEmpty(controller);

            var handler = _handlers.FirstOrDefault(x => x.Metadata.Actions.Contains(action, StringComparer.OrdinalIgnoreCase)
                && x.Metadata.Controller.EqualsNoCase(controller)
                && x.Metadata.Area.NullEmpty().EqualsNoCase(area.NullEmpty()));

            return Convert(handler);
        }

        public virtual CheckoutStep GetNextCheckoutStep(CheckoutStep step, bool next)
        {
            Guard.NotNull(step);

            Lazy<ICheckoutHandler, CheckoutHandlerMetadata> handler;

            if (next)
            {
                handler = _handlers
                    .Where(x => x.Metadata.Order > step.Handler.Metadata.Order)
                    .OrderBy(x => x.Metadata.Order)
                    .FirstOrDefault();
            }
            else
            {
                handler = _handlers
                    .Where(x => x.Metadata.Order < step.Handler.Metadata.Order)
                    .OrderByDescending(x => x.Metadata.Order)
                    .FirstOrDefault();
            }

            return Convert(handler);
        }

        protected virtual CheckoutStep Convert(Lazy<ICheckoutHandler, CheckoutHandlerMetadata> handler)
        {
            if (handler == null)
            {
                return null;
            }

            var viewPath = _isStandardCheckout ? null : $"{_shoppingCartSettings.CheckoutTemplate}.{handler.Metadata.DefaultAction}";

            return new(new(handler), viewPath);
        }
    }
}
