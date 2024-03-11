namespace Smartstore.Core.Checkout.Orders
{
    public class CheckoutFactory : ICheckoutFactory
    {
        private readonly IEnumerable<Lazy<ICheckoutHandler, CheckoutHandlerMetadata>> _handlers;
        private readonly string _checkoutName;

        public CheckoutFactory(IEnumerable<Lazy<ICheckoutHandler, CheckoutHandlerMetadata>> handlers)
        {
            _handlers = handlers.OrderBy(x => x.Metadata.Order);

            // TODO: (mg) get from setting.
            _checkoutName = CheckoutNames.Standard;
        }

        public virtual CheckoutStep[] GetCheckoutSteps()
        {
            var handlers = _handlers
                .Select(x => new CheckoutStep(new(x)))
                .ToArray();

            return handlers;
        }

        public virtual CheckoutStep GetCheckoutStep(string action, string controller, string area = null)
        {
            Guard.NotEmpty(action);
            Guard.NotEmpty(controller);

            var handler = _handlers.FirstOrDefault(x => x.Metadata.Actions.Contains(action, StringComparer.OrdinalIgnoreCase)
                && x.Metadata.Controller.EqualsNoCase(controller)
                && x.Metadata.Area.NullEmpty().EqualsNoCase(area.NullEmpty()));

            return handler != null ? new(new(handler)) : null;
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

            return handler != null ? new(new(handler)) : null;
        }
    }
}
