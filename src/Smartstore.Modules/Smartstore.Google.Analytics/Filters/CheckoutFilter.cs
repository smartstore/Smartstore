using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Google.Analytics.Filters
{
    public class CheckoutFilter : IAsyncActionFilter
    {
        private readonly OrderSettings _orderSettings;

        public CheckoutFilter(OrderSettings orderSettings)
        {
            _orderSettings = orderSettings;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_orderSettings.DisableOrderCompletedPage)
            {
                // Set session var to indicate Order was just being completed.
                // We use plugin prefix in order to remove it safely in case other plugins also must set this.
                context.HttpContext.Session.SetString("GA-OrderCompleted", "true");
            }

            await next();
        }
    }
}
