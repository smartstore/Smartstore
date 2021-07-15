using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Data.Caching;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class CommonController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IMemoryCache _memCache;

        public CommonController(SmartDbContext db, IMemoryCache memCache)
        {
            _db = db;
            _memCache = memCache;
        }

        public async Task<IActionResult> LanguageSelected(int customerlanguage)
        {
            var language = await _db.Languages.FindByIdAsync(customerlanguage, false);
            if (language != null && language.Published)
            {
                Services.WorkContext.WorkingLanguage = language;
            }

            return Content(T("Admin.Common.DataEditSuccess"));
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        public IActionResult RestartApplication(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Services.WebHelper.GetUrlReferrer()?.PathAndQuery;
            return View();
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult RestartApplication()
        {
            Services.WebHelper.RestartAppDomain();
            return new EmptyResult();
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        [HttpPost]
        public async Task<IActionResult> ClearCache()
        {
            // Clear Smartstore inbuilt cache
            await Services.Cache.ClearAsync();

            // Clear IMemoryCache Smartstore: region
            _memCache.RemoveByPattern(_memCache.BuildScopedKey("*"));

            return new JsonResult
            (
                new
                {
                    Success = true,
                    Message = T("Admin.Common.TaskSuccessfullyProcessed").Value
                }
            );
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        [HttpPost]
        public async Task<IActionResult> ClearDatabaseCache()
        {
            var dbCache = _db.GetInfrastructure<IServiceProvider>().GetService<IDbCache>();
            if (dbCache != null)
            {
                await dbCache.ClearAsync();
            }

            return new JsonResult
            (
                new
                {
                    Success = true,
                    Message = T("Admin.Common.TaskSuccessfullyProcessed").Value
                }
            );
        }
    }
}
