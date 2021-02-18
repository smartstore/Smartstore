using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Content.Menus
{
    public class MenuAttribute : TypeFilterAttribute
    {
        public MenuAttribute()
            : base(typeof(MenuFilter))
        {
        }

        class MenuFilter : IAsyncActionFilter, IAsyncResultFilter
        {
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                await next();

                // TODO: (mh) (core) Port MenuActionFilter.OnActionExecuted() here.
            }

            public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
            {
                // TODO: (mh) (core) Port MenuResultFilter.OnResultExecuting() here.

                await next();
            }
        }
    }
}
