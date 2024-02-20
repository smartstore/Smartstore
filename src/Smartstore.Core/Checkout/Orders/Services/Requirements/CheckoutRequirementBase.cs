using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

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

        protected abstract string ActionName { get; }
        protected virtual string ControllerName => "Checkout";

        public abstract int Order { get; }

        public bool IsRequirementFor(string action, string controller = "Checkout")
            => action.EqualsNoCase(ActionName) && controller.EqualsNoCase(ControllerName);

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

        protected bool IsSameRoute(string method, string action, string controller = "Checkout")
        {
            var request = HttpContext.Request;
            return request.Method.EqualsNoCase(method) && request.RouteValues.IsSameRoute(controller, action);
        }
    }
}

