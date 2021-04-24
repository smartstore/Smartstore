using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Security;
using Smartstore.Web.Theming;

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
    [AuthorizeAdmin]
    [AutoValidateAntiforgeryToken]
    //[AdminValidateIpAddress]
    [RequireSsl]
    //[CustomerLastActivity(Order = int.MaxValue)]
    //[StoreIpAddress(Order = int.MaxValue)]
    [AdminThemed]
    public abstract class AdminControllerBase : ManageController
    {
    }
}
