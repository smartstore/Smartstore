using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;

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
}
