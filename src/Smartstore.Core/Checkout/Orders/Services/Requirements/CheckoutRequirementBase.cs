using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Utilities;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public abstract class CheckoutRequirementBase : ICheckoutRequirement
    {
        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected CheckoutRequirementBase(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected HttpContext HttpContext
            => _httpContextAccessor.HttpContext;

        /// <summary>
        /// Gets the name of the action associated with the requirement.
        /// </summary>
        protected abstract string ActionName { get; }

        /// <summary>
        /// Gets the name of the controller associated with the requirement.
        /// </summary>
        protected virtual string ControllerName => "Checkout";

        public abstract int Order { get; }

        public virtual bool IsRequirementFor(string action, string controller)
            => ActionName.EqualsNoCase(action) && controller.EqualsNoCase(ControllerName);

        public virtual Task<CheckoutRequirementResult> CheckAsync(ShoppingCart cart, object model = null)
            => Task.FromResult(new CheckoutRequirementResult(RequirementFulfilled.No));

        public virtual IActionResult Fulfill()
        {
            var request = HttpContext.Request;

            if (request.Method.EqualsNoCase(HttpMethods.Get) && request.RouteValues.IsSameRoute(ControllerName, ActionName))
            {
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

        #region Compare

        public override bool Equals(object obj)
            => Equals(obj as ICheckoutRequirement);

        bool IEquatable<ICheckoutRequirement>.Equals(ICheckoutRequirement other)
            => Equals(other);

        protected virtual bool Equals(ICheckoutRequirement other)
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

