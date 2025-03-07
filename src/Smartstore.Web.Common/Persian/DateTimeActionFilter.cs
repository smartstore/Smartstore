using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Globalization;
using System.Threading;

namespace SmartStore.Web.Framework.Persian
{
    public class DateTimeActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (PersianCulture.IsPersianCulture())
            {
                Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = PersianDateExtensionMethods.GetPersianCulture();
            }
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            // No changes needed here
        }
    }

   
}
