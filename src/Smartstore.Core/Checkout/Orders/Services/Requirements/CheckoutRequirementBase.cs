using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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

        public CheckoutRequirement Requirement { get; }

        public virtual Task<bool> IsFulfilledAsync(ShoppingCart cart)
            => Task.FromResult(false);

        public virtual Task<bool> AdvanceAsync(ShoppingCart cart, object model)
            => Task.FromResult(false);

        public virtual IActionResult Fulfill()
        {
            var result = CheckoutWorkflow.RedirectToCheckout(Requirement.ToString());
            var routeData = _httpContextAccessor.HttpContext.GetRouteData();

            var isCurrentRoute = result.ActionName.EqualsNoCase(routeData.GetActionName()) &&
                result.ControllerName.EqualsNoCase(routeData.GetControllerName()) &&
                result.RouteValues.GetAreaName().EqualsNoCase(routeData.GetAreaName());

            return isCurrentRoute ? null : result;
        }
    }
}
