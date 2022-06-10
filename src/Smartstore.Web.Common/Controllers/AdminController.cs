using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Security;

namespace Smartstore.Web.Controllers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class NonAdminAttribute : ActionFilterAttribute
    {
        public NonAdminAttribute()
        {
            // Must come after AdminThemedAttribute.
            Order = 100;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.RequestServices.GetRequiredService<IWorkContext>().IsAdminArea = false;
        }
    }

    [Area("Admin")]
    [AutoValidateAntiforgeryToken]
    [AuthorizeAdmin]
    [ValidateAdminIpAddress]
    public abstract class AdminController : ManageController
    {
    }
}
