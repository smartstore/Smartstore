using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public abstract class CheckoutRequirementBase : ICheckoutRequirement
    {
        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected CheckoutRequirementBase(CheckoutRequirement requirement, IHttpContextAccessor httpContextAccessor)
        {
            Requirement = requirement;

            _httpContextAccessor = httpContextAccessor;
        }

        protected IActionResult Result
            => CheckoutWorkflow.RedirectToCheckout(Requirement.ToString());

        public CheckoutRequirement Requirement { get; }

        //public virtual IActionResult Fulfill()
        //{
        //    if (_result is RedirectToActionResult redirectResult)
        //    {
        //        var routeData = _httpContextAccessor.HttpContext.GetRouteData();

        //        var isCurrentRoute = redirectResult.ActionName.EqualsNoCase(routeData.GetActionName()) &&
        //            redirectResult.ControllerName.EqualsNoCase(routeData.GetControllerName()) &&
        //            redirectResult.RouteValues.GetAreaName().EqualsNoCase(routeData.GetAreaName());

        //        // return "null" to fulfill self, otherwise fulfill other.
        //        return isCurrentRoute ? null : _result;
        //    }

        //    return _result;
        //}

        public virtual Task<bool> IsFulfilledAsync(ShoppingCart cart)
            => Task.FromResult(false);

        public virtual Task<IActionResult> FulfillAsync(ShoppingCart cart)
            => Task.FromResult<IActionResult>(null);

        public virtual Task<IActionResult> AdvanceAsync(ShoppingCart cart, object model)
            => Task.FromResult<IActionResult>(null);
    }
}
