using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine;
using Smartstore.Threading;
using Smartstore.Web.Infrastructure.Installation;

namespace Smartstore.Web.Controllers
{
    public class InstallController : Controller
    {
        private readonly IInstallationService _installService;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IApplicationContext _appContext;
        private readonly IAsyncState _asyncState;

        public InstallController(
            IHostApplicationLifetime hostApplicationLifetime,
            IApplicationContext appContext,
            IAsyncState asyncState)
        {
            _installService = EngineContext.Current.Scope.ResolveOptional<IInstallationService>();
            _hostApplicationLifetime = hostApplicationLifetime;
            _appContext = appContext;
            _asyncState = asyncState;
        }

        // TODO: (core) Use InstallLogger
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_appContext.IsInstalled)
            {
                context.Result = RedirectToRoute("Homepage");
                return;
            }

            await next();
        }

        public IActionResult Index()
        {
            var model = new InstallationModel
            {
                AdminEmail = _installService.GetResource("AdminEmailValue")
            };

            var curLanguage = _installService.GetCurrentLanguage();

            var installLanguages = _installService.GetInstallationLanguages()
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Value = Url.Action("ChangeLanguage", "Install", new { language = x.Code }),
                        Text = x.Name,
                        Selected = curLanguage.Code == x.Code
                    };
                })
                .ToList();

            var appLanguages = _installService.GetAppLanguages()
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Value = x.Culture,
                        Text = x.Name,
                        Selected = x.UniqueSeoCode.EqualsNoCase(curLanguage.Code)
                    };
                })
                .ToList();

            if (!appLanguages.Any(x => x.Selected))
            {
                appLanguages.FirstOrDefault(x => x.Value.EqualsNoCase("en")).Selected = true;
            }

            ViewBag.AvailableInstallationLanguages = installLanguages;
            ViewBag.AvailableAppLanguages = appLanguages;

            ViewBag.AvailableMediaStorages = new[] 
            {
                new SelectListItem { Value = "fs", Text = _installService.GetResource("MediaStorage.FS"), Selected = true },
                new SelectListItem { Value = "db", Text = _installService.GetResource("MediaStorage.DB") }
            };

            return View(model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> Install(InstallationModel model)
        {
            var result = await _installService.InstallAsync(model, HttpContext.RequestServices.AsLifetimeScope());
            return Json(result);
        }

        [HttpPost]
        public async Task<JsonResult> Progress()
        {
            var progress = await _asyncState.GetAsync<InstallationResult>();
            return Json(progress);
        }

        [HttpPost]
        public async Task<IActionResult> Finalize(bool restart)
        {
            await _asyncState.RemoveAsync<InstallationResult>();

            if (restart)
            {
                _hostApplicationLifetime.StopApplication();
            }

            return Json(new { Success = true });
        }

        public IActionResult ChangeLanguage(string language)
        {
            _installService.SaveCurrentLanguage(language);
            return RedirectToAction("Index");
        }

        [IgnoreAntiforgeryToken]
        public IActionResult RestartInstall()
        {
            //// Restart application
            //_hostApplicationLifetime.StopApplication();

            // Redirect to home page
            return RedirectToAction("Index");
        }
    }
}
