using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Is always being published right before a view component is about to render the view.
    /// This event sort of replaces "OnActionExecuted" or "OnResultExecuting" child action filters of classic MVC.
    /// </summary>
    public class ViewComponentResultExecutingEvent : ViewComponentEventBase
    {
        public ViewComponentResultExecutingEvent(ViewComponentContext context, ViewViewComponentResult result)
            : base(context)
        {
            Guard.NotNull(result, nameof(result));

            Result = result;
        }

        public ViewViewComponentResult Result { get; set; }

        public string ViewName
        {
            get => Result.ViewName;
            set => Result.ViewName = value;
        }

        public object Model
        {
            get => Result.ViewData.Model;
            set => Result.ViewData.Model = value;
        }
    }
}