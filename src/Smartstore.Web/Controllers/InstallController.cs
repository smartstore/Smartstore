using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using Smartstore.Core.Installation;
using Smartstore.Threading;

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
            if (!ModelState.IsValid)
            {
                var result = new InstallationResult();
                ModelState.SelectMany(x => x.Value.Errors).Each(x => result.Errors.Add(x.ErrorMessage));
                return Json(result);
            }
            else
            {
                var result = await _installService.InstallAsync(model, HttpContext.RequestServices.AsLifetimeScope());
                return Json(result);
            }
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
            // Redirect to home page
            return RedirectToAction("Index");
        }
    }
}
