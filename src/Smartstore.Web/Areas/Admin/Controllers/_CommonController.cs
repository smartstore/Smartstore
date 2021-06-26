using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Smartstore.Caching;
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
        private readonly IDbCache _dbCache;

        // TODO: (mh) (core) dbCache cannot be resolved. Breaks...
        public CommonController(SmartDbContext db, IMemoryCache memCache/*, IDbCache dbCache*/)
        {
            _db = db;
            _memCache = memCache;
            //_dbCache = dbCache;
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
        [HttpPost]
        public IActionResult RestartApplication()
        {
            // TODO: (mh) (core) This must be tested in production environment. In VS _hostApplicationLifetime.StopApplication() just stops without restarting on next request.
            Services.WebHelper.RestartAppDomain();

            return new JsonResult(null);
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        [HttpPost]
        public IActionResult ClearCache()
        {
            // Clear Smartstore inbuilt cache
            Services.Cache.Clear();

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
        public ActionResult ClearDatabaseCache()
        {
            // TODO: (mh) (core) Uncomment when dbCache can be resolved.
            //_dbCache.Clear();

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
