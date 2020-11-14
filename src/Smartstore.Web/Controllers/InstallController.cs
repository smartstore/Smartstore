using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Engine;

namespace Smartstore.Web.Controllers
{
    public class InstallController : Controller
    {
        private readonly IApplicationContext _appContext;
        
        public InstallController(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_appContext.IsInstalled)
            {
                context.Result = RedirectToRoute("Homepage");
                return;
            }

            await next();
        }

        public Task<IActionResult> Index()
        {
            return Task.FromResult<IActionResult>(View());
        }
    }
}
