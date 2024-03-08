using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Utilities;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    public abstract class CheckoutHandlerBase : ICheckoutHandler
    {
        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected CheckoutHandlerBase(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected HttpContext HttpContext
            => _httpContextAccessor.HttpContext;

        /// <summary>
        /// Gets the name of the action method associated with the handler.
        /// </summary>
        protected abstract string ActionName { get; }

        /// <summary>
        /// Gets the name of the controller associated with the handler.
        /// </summary>
        protected virtual string ControllerName => "Checkout";

        public abstract int Order { get; }

        public virtual bool IsHandlerFor(string action, string controller)
            => ActionName.EqualsNoCase(action) && controller.EqualsNoCase(ControllerName);

        public virtual Task<CheckoutHandlerResult> ProcessAsync(ShoppingCart cart, object model = null)
            => Task.FromResult(new CheckoutHandlerResult(false));

        public virtual IActionResult GetActionResult()
        {
            var request = HttpContext.Request;

            if (request.Method.EqualsNoCase(HttpMethods.Get) && request.RouteValues.IsSameRoute(ControllerName, ActionName))
            {
                // TODO: (mg) Why not call IsSameRoute method?
                // Avoid infinite redirection loop.
                return null;
            }

            return new RedirectToActionResult(ActionName, ControllerName, null);
        }

        /// <summary>
        /// Gets a value indicating whether the current request corresponds to a specific route.
        /// </summary>
        protected bool IsSameRoute(string method, string action, string controller = "Checkout")
        {
            var request = HttpContext.Request;
            return request.Method.EqualsNoCase(method) && request.RouteValues.IsSameRoute(controller, action);
        }

        /// <summary>
        /// Gets a request form value.
        /// </summary>
        // TODO: (mg) Pass CheckoutContext.
        protected string GetFormValue(string key)
            => HttpContext.Request.Form.TryGetValue(key, out var val) ? val.ToString() : null;

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
                .Add(ActionName)
                .Add(ControllerName);

            return combiner.CombinedHash;
        }

        #endregion
    }
}

