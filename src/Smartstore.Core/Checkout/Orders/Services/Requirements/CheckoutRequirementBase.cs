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

        protected virtual string Area => null;

        public abstract int Order { get; }

        public bool IsRequirementFor(string action, string controller = "Checkout", string area = null)
            => action.EqualsNoCase(ActionName) && controller.EqualsNoCase(ControllerName) && area.EqualsNoCase(Area);

        public virtual Task<(bool Fulfilled, CheckoutWorkflowError[] Errors)> IsFulfilledAsync(ShoppingCart cart, object model = null)
            => Task.FromResult<(bool, CheckoutWorkflowError[])>((false, null));

        public virtual IActionResult Fulfill()
        {
            var request = HttpContext.Request;

            if (request.Method.EqualsNoCase(HttpMethods.Get) && request.RouteValues.IsSameRoute(Area, ControllerName, ActionName))
            {
                // Avoid infinite redirection loop.
                return null;
            }

            return new RedirectToActionResult(ActionName, ControllerName, Area == null ? null : new { area = Area });
        }

        protected bool IsSameRoute(string method, string action, string controller = "Checkout", string area = null)
        {
            var request = HttpContext.Request;
            return request.Method.EqualsNoCase(method) && request.RouteValues.IsSameRoute(area, controller, action);
        }
    }
}

