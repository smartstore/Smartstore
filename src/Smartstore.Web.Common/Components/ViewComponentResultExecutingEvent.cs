using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Is always being published right before a view component is about to render the view.
    /// This event sort of replaces "OnActionExecuted" or "OnResultExecuting" child action filters of classic MVC.
    /// </summary>
    public class ViewComponentResultExecutingEvent : ViewComponentEventBase
    {
        public ViewComponentResultExecutingEvent(ViewComponentContext context, IViewComponentResult result)
            : base(context)
        {
            Result = Guard.NotNull(result);
        }

        public IViewComponentResult Result { get; set; }
    }
}