using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Is being published right before a view component is about to create/prepare its model.
    /// This event sort of replaces "OnActionExecuting" child action filter of classic MVC.
    /// Unlike <see cref="ViewComponentResultExecutingEvent"/>, which is ALWAYS published implicitly
    /// by <see cref="SmartViewComponent"/>, this event must be explicitly published by view component authors
    /// at the most suitable code location (preferably right before model creation). The component
    /// author should then check whether the <see cref="Model"/> property has been assigned a non-null value by any event consumer.
    /// The author should skip model creation in that case and continue with externally provided <see cref="Model"/> instead.
    /// </summary>
    public class ViewComponentExecutingEvent<TModel> : ViewComponentEventBase
    {
        public ViewComponentExecutingEvent(ViewComponentContext context)
            : base(context)
        {
        }

        public TModel Model { get; set; }
    }
}