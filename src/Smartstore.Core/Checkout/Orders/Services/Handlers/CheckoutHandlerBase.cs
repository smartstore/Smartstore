using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Utilities;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    public abstract class CheckoutHandlerBase : ICheckoutHandler
    {
        /// <summary>
        /// Gets the name of the action method associated with the handler.
        /// </summary>
        protected abstract string Action { get; }

        /// <summary>
        /// Gets the name of the controller associated with the handler.
        /// </summary>
        protected virtual string Controller => "Checkout";

        protected virtual string Area => null;

        public abstract int Order { get; }

        public virtual bool IsHandlerFor(CheckoutContext context)
            => context.IsCurrentRoute(null, Action, Controller, Area);

        protected bool IsHandlerFor(string[] actions, CheckoutContext context)
        {
            return actions.Contains(context.Route.GetActionName(), StringComparer.OrdinalIgnoreCase)
                && Controller.EqualsNoCase(context.Route.GetControllerName())
                && Area.NullEmpty().EqualsNoCase(context.Route.GetAreaName().NullEmpty());
        }

        public virtual Task<CheckoutHandlerResult> ProcessAsync(CheckoutContext context)
            => Task.FromResult(new CheckoutHandlerResult(false));

        public virtual IActionResult GetActionResult(CheckoutContext context)
        {
            if (context.IsCurrentRoute(HttpMethods.Get, Controller, Action, Area))
            {
                // Avoid infinite redirection loop.
                return null;
            }

            return new RedirectToActionResult(Action, Controller, Area.HasValue() ? new { area = Area } : null);
        }

        #region Compare

        public override bool Equals(object obj)
            => Equals(obj as ICheckoutHandler);

        bool IEquatable<ICheckoutHandler>.Equals(ICheckoutHandler other)
            => Equals(other);

        protected virtual bool Equals(ICheckoutHandler other)
            => GetHashCode() == other.GetHashCode();

        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner
                .Start()
                .Add(Order)
                .Add(Action)
                .Add(Controller)
                .Add(Area);

            return combiner.CombinedHash;
        }

        #endregion
    }
}

