using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public abstract class CheckoutRequirementBase : ICheckoutRequirement
    {
        protected readonly IActionContextAccessor _actionContextAccessor;

        protected CheckoutRequirementBase(IActionContextAccessor actionContextAccessor)
        {
            _actionContextAccessor = actionContextAccessor;
        }

        public abstract int Order { get; }

        protected HttpContext HttpContext
            => _actionContextAccessor.ActionContext.HttpContext;

        protected abstract RedirectToActionResult FulfillResult { get; }

        public virtual Task<bool> IsFulfilledAsync(ShoppingCart cart, object model = null)
            => Task.FromResult(false);

        public virtual IActionResult Fulfill()
        {
            var request = HttpContext.Request;
            var result = FulfillResult;

            var doNotRedirect = request.RouteValues.IsSameRoute(result.RouteValues.GetAreaName(), result.ControllerName, result.ActionName) &&
                (request.Method.EqualsNoCase(HttpMethods.Get) || 
                (request.Method.EqualsNoCase(HttpMethods.Post) && !_actionContextAccessor.ActionContext.ModelState.IsValid));

            return doNotRedirect ? null : result;
        }

        protected bool IsSameRoute(string method, string action, string controller = "Checkout", string area = null)
        {
            var request = HttpContext.Request;
            return request.Method.EqualsNoCase(method) && request.RouteValues.IsSameRoute(area, controller, action);
        }
    }
}
