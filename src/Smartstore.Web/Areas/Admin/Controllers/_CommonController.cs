using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Admin.Models.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Data.Caching;
using Smartstore.Utilities;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class CommonController : AdminController
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
        [HttpPost]
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

        [Permission(Permissions.System.Maintenance.Read)]
        public async Task<IActionResult> SystemInfo()
        {
            var runtimeInfo = Services.ApplicationContext.RuntimeInfo;
            var dataProvider = Services.DbContext.DataProvider;

            var model = new SystemInfoModel
            {
                AppVersion = SmartstoreVersion.CurrentFullVersion,
                ServerLocalTime = DateTime.Now,
                UtcTime = DateTime.UtcNow,
                HttpHost = Request.Host.ToString(),
                ServerTimeZone = TimeZoneInfo.Local.StandardName,
                AspNetInfo = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OperatingSystem = $"{runtimeInfo.OSDescription} ({runtimeInfo.ProcessArchitecture.ToString().ToLower()})"
            };

            // DB size & used RAM
            try
            {
                var mbSize = await dataProvider.GetDatabaseSizeAsync();
                model.DatabaseSize = Convert.ToInt64(mbSize * 1024 * 1024);
                model.UsedMemorySize = GetPrivateBytes();
            }
            catch
            {
            }

            // DB settings
            try
            {
                if (DataSettings.Instance.IsValid())
                {
                    model.DataProviderFriendlyName = DataSettings.Instance.DbFactory.DbSystem.ToString();
                    model.ShrinkDatabaseEnabled = dataProvider.CanShrink && Services.Permissions.Authorize(Permissions.System.Maintenance.Read);
                }
            }
            catch
            {
            }

            // Loaded assemblies
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fi = new FileInfo(assembly.Location);
                model.AppDate = fi.LastWriteTime.ToLocalTime();
            }
            catch 
            { 
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var loadedAssembly = new SystemInfoModel.LoadedAssembly
                {
                    FullName = assembly.FullName
                };

                if (!assembly.IsDynamic)
                {
                    try
                    {
                        loadedAssembly.Location = assembly.Location;
                    }
                    catch
                    {

                    }
                }

                model.LoadedAssemblies.Add(loadedAssembly);
            }

            // MemCache stats
            model.MemoryCacheStats = GetMemoryCacheStats();

            return View(model);
        }

        #region Utils

        /// <summary>
        /// Counts the size of all objects in both IMemoryCache and Smartstore memory cache
        /// </summary>
        private IDictionary<string, long> GetMemoryCacheStats()
        {
            var stats = new Dictionary<string, long>();
            return stats;
            var instanceLookups = new HashSet<object>(ReferenceEqualityComparer.Instance);

            // IMemoryCache
            var memCacheKeys = _memCache.EnumerateKeys().ToArray();
            foreach (var key in memCacheKeys)
            {
                var value = _memCache.Get(key);
                var size = GetObjectSize(value);

                if (key is string str)
                {
                    stats.Add("MemoryCache:" + str.Replace(':', '_'), size + Encoding.Default.GetByteCount(str));
                }
                else
                {
                    stats.Add("MemoryCache:" + key.ToString(), size + GetObjectSize(key));
                }
            }

            // Smartstore CacheManager
            var cache = Services.CacheFactory.GetMemoryCache();
            var cacheKeys = cache.Keys("*").ToArray();
            foreach (var key in cacheKeys)
            {
                var value = cache.Get<object>(key);
                var size = GetObjectSize(value);

                stats.Add(key, size + Encoding.Default.GetByteCount(key));
            }

            return stats;

            long GetObjectSize(object obj)
            {
                if (obj == null)
                {
                    return 0;
                }
                
                try
                {
                    return CommonHelper.GetObjectSizeInBytes(obj, instanceLookups);
                }
                catch
                {
                    return 0;
                }
            }
        }

        private static long GetPrivateBytes()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var process = Process.GetCurrentProcess();
            process.Refresh();

            return process.PrivateMemorySize64;
        }

        #endregion
    }
}
