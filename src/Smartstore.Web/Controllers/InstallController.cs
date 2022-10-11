using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Smartstore.Core.Installation;
using Smartstore.IO;
using Smartstore.Threading;
using Smartstore.Utilities;

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

        private string T(string resourceName)
        {
            return _installService.GetResource(resourceName);
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

        private void PrepareInstallForm()
        {
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

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var dataProviders = new List<SelectListItem>
            {
                new SelectListItem { Value = "mysql", Text = T("UseMySql"), Selected = !isWindows },
                new SelectListItem { Value = "sqlserver", Text = T("UseSqlServer"), Selected = isWindows }
            };

            ViewBag.AvailableInstallationLanguages = installLanguages;
            ViewBag.AvailableAppLanguages = appLanguages;
            ViewBag.AvailableDataProviders = dataProviders;

            ViewBag.AvailableMediaStorages = new[]
            {
                new SelectListItem { Value = "fs", Text = T("MediaStorage.FS") + " " + T("Recommended"), Selected = true },
                new SelectListItem { Value = "db", Text = T("MediaStorage.DB") }
            };
        }

        public async Task<IActionResult> Index(bool noAutoInstall = false)
        {
            // TODO: (core) Call CallbackUrl

            var result = _installService.GetCurrentInstallationResult();
            if (result != null)
            {
                // Install already running, we gonna need the result in UI
                ViewBag.InstallResult = result;

                PrepareInstallForm();
                return View(result.Model);
            }

            if (!noAutoInstall && TryGetAutoInstallModel(out var model, out var file))
            {
                if (!TryValidateModel(model))
                {
                    // Must validate model explicitly, no model binding here.
                    PrepareInstallForm();
                    return View(model);
                }
                
                // Do AutoInstall!
                result = await _installService.InstallAsync(model, HttpContext.RequestServices.AsLifetimeScope());

                if (!result.Completed || result.HasErrors)
                {
                    await _asyncState.RemoveAsync<InstallationResult>();
                }

                if (result.Completed && result.Success)
                {
                    if (!HttpContext.Connection.IsLocal() && !CommonHelper.IsDevEnvironment)
                    {
                        // Delete auto-install model file after successfull installation
                        file.Delete();
                    }  

                    // Shutdown application with slight delay to not
                    // interfere with AJAX calls.
                    _ = Task.Delay(500).ContinueWith((t, state) => 
                    {
                        ((IHostApplicationLifetime)state).StopApplication();
                    }, _hostApplicationLifetime);
                }

                return Json(result);
            }
            
            // From here on we know it is NOT autoinstall...

            model = result?.Model ?? new InstallationModel
            {
                AdminEmail = T("AdminEmailValue")
            };

            PrepareInstallForm();
            return View(model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> Install(InstallationModel model)
        {
            if (!ModelState.IsValid)
            {
                var result = new InstallationResult(model);

                CopyModelErrors(result);

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

            if (restart && 
                !_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested && 
                !_hostApplicationLifetime.ApplicationStopped.IsCancellationRequested)
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

        private bool TryGetAutoInstallModel(out InstallationModel model, out IFile file)
        {
            model = null;
            file = null;

            try
            {
                file = _appContext.AppDataRoot.GetFile("installmodel.json");
                if (file.Exists)
                {
                    var json = file.ReadAllText();
                    model = JsonConvert.DeserializeObject<InstallationModel>(json);

                    // Otherwise validation fails
                    model.ConfirmPassword = model.AdminPassword;

                    model.IsAutoInstall = true;
                }
            }
            catch
            {
            }

            return model != null;
        }

        private void CopyModelErrors(InstallationResult result)
        {
            foreach (var modelError in ModelState.SelectMany(x => x.Value.Errors))
            {
                result.Errors.Add(modelError.ErrorMessage);
            }
        }
    }
}
