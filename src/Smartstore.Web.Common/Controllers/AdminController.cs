using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
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
    [RequireSsl]
    [TrackActivity(Order = 100)]
    [SaveChanges(typeof(SmartDbContext), Order = int.MaxValue)]
    public abstract class AdminController : ManageController
    {
    }
}
