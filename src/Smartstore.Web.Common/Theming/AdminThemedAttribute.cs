using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Theming
{
    /// <summary>
    /// Instructs the view engine to additionally search in the admin area for views.
    /// </summary>
    /// <remarks>
    /// The "admin area" corresponds to the <c>/Areas/Admin/</c> base folder.
    /// This attribute is useful in modules - which usually are areas on its own - where views
    /// should be rendered as part of the admin backend.
    /// Without this attribute the view resolver would directly fallback to the default nameless area
    /// when a view could not be resolved from within the module area.
    /// </remarks>
    public class AdminThemedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.RequestServices.GetRequiredService<IWorkContext>().IsAdminArea = true;
        }
    }
}
