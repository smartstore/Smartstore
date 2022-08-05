using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Smartstore.Admin.Models.Maintenance;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Identity;
using Smartstore.Core.Logging;
using Smartstore.Core.Packaging;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Data.Caching;
using Smartstore.Http;
using Smartstore.Imaging;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Utilities;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class MaintenanceController : AdminController
    {
        private const string BACKUP_DIR = "DbBackups";

        private readonly SmartDbContext _db;
        private readonly IMemoryCache _memCache;
        private readonly ITaskScheduler _taskScheduler;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ICustomerService _customerService;
        private readonly IImageFactory _imageFactory;
        private readonly Lazy<IImageCache> _imageCache;
        private readonly Lazy<IFilePermissionChecker> _filePermissionChecker;
        private readonly Lazy<ICurrencyService> _currencyService;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly Lazy<IShippingService> _shippingService;
        private readonly Lazy<IExportProfileService> _exportProfileService;
        private readonly Lazy<IImportProfileService> _importProfileService;
        private readonly Lazy<UpdateChecker> _updateChecker;
        private readonly MeasureSettings _measureSettings;

        public MaintenanceController(
            SmartDbContext db,
            IMemoryCache memCache,
            ITaskScheduler taskScheduler,
            IHttpClientFactory httpClientFactory,
            IHostApplicationLifetime hostApplicationLifetime,
            ICustomerService customerService,
            IImageFactory imageFactory,
            Lazy<IImageCache> imageCache,
            Lazy<IFilePermissionChecker> filePermissionChecker,
            Lazy<ICurrencyService> currencyService,
            Lazy<IPaymentService> paymentService,
            Lazy<IShippingService> shippingService,
            Lazy<IExportProfileService> exportProfileService,
            Lazy<IImportProfileService> importProfileService,
            Lazy<UpdateChecker> updateChecker,
            MeasureSettings measureSettings)
        {
            _db = db;
            _memCache = memCache;
            _taskScheduler = taskScheduler;
            _httpClientFactory = httpClientFactory;
            _hostApplicationLifetime = hostApplicationLifetime;
            _customerService = customerService;
            _imageFactory = imageFactory;
            _imageCache = imageCache;
            _filePermissionChecker = filePermissionChecker;
            _currencyService = currencyService;
            _paymentService = paymentService;
            _shippingService = shippingService;
            _exportProfileService = exportProfileService;
            _importProfileService = importProfileService;
            _updateChecker = updateChecker;
            _measureSettings = measureSettings;
        }

        #region Maintenance

        [Permission(Permissions.System.Maintenance.Read)]
        public async Task<IActionResult> Index()
        {
            var model = new MaintenanceModel
            {
                CanExecuteSql = _db.DataProvider.CanExecuteSqlScript,
                CanCreateBackup = _db.DataProvider.CanBackup
            };

            model.DeleteGuests.EndDate = DateTime.UtcNow.AddDays(-7);
            model.DeleteGuests.OnlyWithoutShoppingCart = true;

            // Image cache stats
            var (fileCount, totalSize) = await _imageCache.Value.CacheStatisticsAsync();
            model.DeleteImageCache.NumFiles = fileCount;
            model.DeleteImageCache.TotalSize = Prettifier.HumanizeBytes(totalSize);

            return View(model);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("delete-image-cache")]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> DeleteImageCache()
        {
            await _imageCache.Value.ClearAsync();
            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("delete-guests")]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> DeleteGuestAccounts(MaintenanceModel model)
        {
            var dtHelper = Services.DateTimeHelper;

            DateTime? startDateValue = model.DeleteGuests.StartDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.DeleteGuests.StartDate.Value, dtHelper.CurrentTimeZone);

            DateTime? endDateValue = model.DeleteGuests.EndDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.DeleteGuests.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

            var numDeletedCustomers = await _customerService.DeleteGuestCustomersAsync(
                startDateValue,
                endDateValue,
                model.DeleteGuests.OnlyWithoutShoppingCart);

            NotifyInfo(T("Admin.System.Maintenance.DeleteGuests.TotalDeleted", numDeletedCustomers));

            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("delete-export-files")]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> DeleteExportFiles(MaintenanceModel model)
        {
            var dtHelper = Services.DateTimeHelper;

            DateTime? startDateUtc = model.DeleteExportedFiles.StartDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.DeleteExportedFiles.StartDate.Value, dtHelper.CurrentTimeZone);

            DateTime? endDateUtc = model.DeleteExportedFiles.EndDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.DeleteExportedFiles.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

            var (numFiles, numFolders) = await _exportProfileService.Value.DeleteExportFilesAsync(startDateUtc, endDateUtc);

            // Also delete unused import profile folders.
            numFolders += await _importProfileService.Value.DeleteUnusedImportDirectoriesAsync();

            NotifyInfo(T("Admin.System.Maintenance.DeletedExportFilesAndFolders", numFiles, numFolders));

            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("execute-sql-query")]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> ExecuteSql(MaintenanceModel model)
        {
            if (_db.DataProvider.CanExecuteSqlScript && model.SqlQuery.HasValue())
            {
                try
                {
                    var rowsAffected = await _db.DataProvider.ExecuteSqlScriptAsync(model.SqlQuery);
                    NotifySuccess(T("Admin.System.Maintenance.SqlQuery.Succeeded", rowsAffected));
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                    return View(model);
                }
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region Update

        [Permission(Permissions.System.Maintenance.Read)]
        public async Task<IActionResult> CheckUpdate(bool enforce = false)
        {
            var model = await _updateChecker.Value.CheckUpdateAsync(enforce);
            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> CheckUpdateSuppress(string myVersion, string newVersion)
        {
            await _updateChecker.Value.SuppressMessageAsync(myVersion, newVersion);
            return Json(new { Success = true });
        }

        // TODO: (core) Think about whether "InstallUpdate" does still makes sense?

        #endregion

        #region Common

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
            _hostApplicationLifetime.StopApplication();
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

        #endregion

        #region SystemInfo

        [Permission(Permissions.System.Maintenance.Read)]
        public async Task<IActionResult> SystemInfo()
        {
            var runtimeInfo = Services.ApplicationContext.RuntimeInfo;
            var dataProvider = _db.DataProvider;

            var model = new SystemInfoModel
            {
                AppVersion = SmartstoreVersion.CurrentFullVersion,
                ServerLocalTime = DateTime.Now,
                UtcTime = DateTime.UtcNow,
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

            //// MemCache stats
            //model.MemoryCacheStats = GetMemoryCacheStats();

            return View(model);
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> GarbageCollect()
        {
            try
            {
                _imageFactory.ReleaseMemory();
                await Task.Delay(500);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                await Task.Delay(500);

                NotifySuccess(T("Admin.System.SystemInfo.GarbageCollectSuccessful"));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToReferrer();
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> ShrinkDatabase()
        {
            try
            {
                if (_db.DataProvider.CanShrink)
                {
                    await _db.DataProvider.ShrinkDatabaseAsync();
                    NotifySuccess(T("Common.ShrinkDatabaseSuccessful"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToReferrer();
        }

        #endregion

        #region Warnings

        [Permission(Permissions.System.Maintenance.Read)]
        public async Task<IActionResult> Warnings()
        {
            var model = new List<SystemWarningModel>();
            var store = Services.StoreContext.CurrentStore;
            var appContext = Services.ApplicationContext;

            // Store URL
            // ====================================
            var storeUrl = store.Url.EnsureEndsWith('/');
            if (storeUrl.HasValue() && (storeUrl.EqualsNoCase(Services.WebHelper.GetStoreLocation(false)) || storeUrl.EqualsNoCase(Services.WebHelper.GetStoreLocation(true))))
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.URL.Match"));
            }
            else
            {
                AddEntry(SystemWarningLevel.Warning, T("Admin.System.Warnings.URL.NoMatch", storeUrl, Services.WebHelper.GetStoreLocation(false)));
            }

            // TaskScheduler reachability
            // ====================================
            try
            {
                var taskSchedulerClient = await _taskScheduler.CreateHttpClientAsync();
                taskSchedulerClient.Timeout = TimeSpan.FromSeconds(5);

                using var response = await taskSchedulerClient.GetAsync("noop");
                response.EnsureSuccessStatusCode();

                var status = response.StatusCode;
                var warningModel = new SystemWarningModel
                {
                    Level = (status == HttpStatusCode.OK ? SystemWarningLevel.Pass : SystemWarningLevel.Fail)
                };

                if (status == HttpStatusCode.OK)
                {
                    warningModel.Text = T("Admin.System.Warnings.TaskScheduler.OK");
                }
                else
                {
                    warningModel.Text = T("Admin.System.Warnings.TaskScheduler.Fail", _taskScheduler.BaseUrl, status + " - " + status.ToString());
                }

                model.Add(warningModel);
            }
            catch (Exception ex)
            {
                var msg = T("Admin.System.Warnings.TaskScheduler.Fail", _taskScheduler.BaseUrl, ex.Message);
                AddEntry(SystemWarningLevel.Fail, msg);
                Logger.Error(ex, msg);
            }

            // Sitemap reachability
            // ====================================
            string sitemapUrl = null;
            try
            {
                var sitemapClient = _httpClientFactory.CreateClient();
                sitemapClient.Timeout = TimeSpan.FromSeconds(15);

                sitemapUrl = WebHelper.GetAbsoluteUrl(Url.Content("sitemap.xml"), Request);
                var uri = await WebHelper.CreateUriForSafeLocalCallAsync(new Uri(sitemapUrl));

                using var response = await sitemapClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                var status = response.StatusCode;
                var warningModel = new SystemWarningModel
                {
                    Level = (status == HttpStatusCode.OK ? SystemWarningLevel.Pass : SystemWarningLevel.Warning)
                };

                switch (status)
                {
                    case HttpStatusCode.OK:
                        warningModel.Text = T("Admin.System.Warnings.SitemapReachable.OK");
                        break;
                    default:
                        if (status == HttpStatusCode.MethodNotAllowed)
                            warningModel.Text = T("Admin.System.Warnings.SitemapReachable.MethodNotAllowed");
                        else
                            warningModel.Text = T("Admin.System.Warnings.SitemapReachable.Wrong");

                        warningModel.Text = string.Concat(warningModel.Text, " ", T("Admin.Common.HttpStatus", (int)status, status.ToString()));
                        break;
                }

                model.Add(warningModel);
            }
            catch (Exception ex)
            {
                AddEntry(SystemWarningLevel.Warning, T("Admin.System.Warnings.SitemapReachable.Wrong"));
                Logger.Warn(ex, T("Admin.System.Warnings.SitemapReachable.Wrong"));
            }

            // Primary exchange rate currency
            // ====================================
            var perCurrency = _currencyService.Value.PrimaryExchangeCurrency;
            if (perCurrency != null)
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.ExchangeCurrency.Set"));

                if (perCurrency.Rate != 1)
                {
                    AddEntry(SystemWarningLevel.Fail, T("Admin.System.Warnings.ExchangeCurrency.Rate1"));
                }
            }
            else
            {
                AddEntry(SystemWarningLevel.Fail, T("Admin.System.Warnings.ExchangeCurrency.NotSet"));
            }

            // Primary store currency
            // ====================================
            var pscCurrency = _currencyService.Value.PrimaryCurrency;
            if (pscCurrency != null)
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.PrimaryCurrency.Set"));
            }
            else
            {
                AddEntry(SystemWarningLevel.Fail, T("Admin.System.Warnings.PrimaryCurrency.NotSet"));
            }


            // Base measure weight
            // ====================================
            var baseWeight = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            if (baseWeight != null)
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.DefaultWeight.Set"));

                if (baseWeight.Ratio != 1)
                {
                    AddEntry(SystemWarningLevel.Fail, T("Admin.System.Warnings.DefaultWeight.Ratio1"));
                }
            }
            else
            {
                AddEntry(SystemWarningLevel.Fail, T("Admin.System.Warnings.DefaultWeight.NotSet"));
            }


            // Base dimension weight
            // ====================================
            var baseDimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId, false);
            if (baseDimension != null)
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.DefaultDimension.Set"));

                if (baseDimension.Ratio != 1)
                {
                    AddEntry(SystemWarningLevel.Fail, T("Admin.System.Warnings.DefaultDimension.Ratio1"));
                }
            }
            else
            {
                AddEntry(SystemWarningLevel.Fail, T("Admin.System.Warnings.DefaultDimension.NotSet"));
            }

            // Shipping rate coputation methods
            // ====================================
            int numActiveShippingMethods = 0;
            try
            {
                numActiveShippingMethods = _shippingService.Value.LoadActiveShippingRateComputationMethods()
                    .Where(x => x.Value.ShippingRateComputationMethodType == ShippingRateComputationMethodType.Offline)
                    .Count();
            }
            catch
            {
            }

            if (numActiveShippingMethods > 1)
            {
                AddEntry(SystemWarningLevel.Warning, T("Admin.System.Warnings.Shipping.OnlyOneOffline"));
            }

            // Payment methods
            // ====================================
            int numActivePaymentMethods = 0;
            try
            {
                numActivePaymentMethods = (await _paymentService.Value.LoadActivePaymentMethodsAsync()).Count();
            }
            catch
            {
            }

            if (numActivePaymentMethods > 0)
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.PaymentMethods.OK"));
            }
            else
            {
                AddEntry(SystemWarningLevel.Fail, T("Admin.System.Warnings.PaymentMethods.NoActive"));
            }

            // Incompatible modules
            // ====================================
            foreach (var moduleName in appContext.ModuleCatalog.IncompatibleModules)
            {
                AddEntry(SystemWarningLevel.Warning, T("Admin.System.Warnings.IncompatiblePlugin", moduleName));
            }

            // Validate write permissions (the same procedure like during installation)
            // ====================================
            var dirPermissionsOk = true;
            foreach (var subpath in FilePermissionChecker.WrittenDirectories)
            {
                var entry = appContext.ContentRoot.GetDirectory(subpath);
                if (entry.Exists && !_filePermissionChecker.Value.CanAccess(entry, FileEntryRights.Write | FileEntryRights.Modify))
                {
                    AddEntry(SystemWarningLevel.Warning, T("Admin.System.Warnings.DirectoryPermission.Wrong", appContext.OSIdentity.Name, subpath));
                    dirPermissionsOk = false;
                }
            }
            if (dirPermissionsOk)
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.DirectoryPermission.OK"));
            }

            var filePermissionsOk = true;
            foreach (var subpath in FilePermissionChecker.WrittenFiles)
            {
                var entry = appContext.ContentRoot.GetFile(subpath);
                if (entry.Exists && !_filePermissionChecker.Value.CanAccess(entry, FileEntryRights.Write | FileEntryRights.Modify | FileEntryRights.Delete))
                {
                    AddEntry(SystemWarningLevel.Warning, T("Admin.System.Warnings.FilePermission.Wrong", appContext.OSIdentity.Name, subpath));
                    filePermissionsOk = false;
                }
            }
            if (filePermissionsOk)
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.FilePermission.OK"));
            }

            return View(model);

            void AddEntry(SystemWarningLevel level, string text)
            {
                model.Add(new SystemWarningModel { Level = level, Text = text });
            }
        }

        #endregion

        #region Database backup

        [Permission(Permissions.System.Maintenance.Read)]
        public async Task<IActionResult> BackupList(GridCommand command)
        {
            var root = Services.ApplicationContext.TenantRoot;
            await root.TryCreateDirectoryAsync(BACKUP_DIR);

            var backups = await root
                .EnumerateFilesAsync(BACKUP_DIR)
                .AsyncToList();

            var dataProvider = _db.DataProvider;

            var rows = backups
                .Select(x =>
                {
                    var validationResult = dataProvider.ValidateBackupFileName(x.Name);
                    if (!validationResult.IsValid)
                    {
                        return null;
                    }

                    var model = new DbBackupModel(x)
                    {
                        Version = validationResult?.Version ?? new Version(),
                        MatchesCurrentVersion = validationResult.MatchesCurrentVersion,
                        CreatedOn = x.CreatedOn.LocalDateTime,
                        DownloadUrl = Url.Action(nameof(DownloadBackup), new { name = x.Name })
                    };

                    return model;
                })
                .AsQueryable()
                .Where(x => x != null)
                .ApplyGridCommand(command)
                .ToList();

            return Json(new GridModel<DbBackupModel>
            {
                Rows = rows,
                Total = rows.Count
            });
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("execute-create-backup")]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                if (_db.DataProvider.CanBackup)
                {
                    var dir = await Services.ApplicationContext.TenantRoot.GetDirectoryAsync(BACKUP_DIR);
                    var fs = dir.FileSystem;

                    var backupName = _db.DataProvider.CreateBackupFileName();
                    var path = PathUtility.Join(dir.SubPath, backupName);

                    var fullPath = fs.CheckUniqueFileName(path, out var newPath)
                        ? fs.MapPath(newPath)
                        : fs.MapPath(path);

                    await _db.DataProvider.BackupDatabaseAsync(fullPath);

                    NotifyInfo(T("Admin.System.Maintenance.DbBackup.BackupCreated"));
                }
                else
                {
                    NotifyError(T("Admin.System.Maintenance.DbBackup.BackupNotSupported", _db.DataProvider.ProviderType.ToString().NaIfEmpty()));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> UploadBackup()
        {
            var uploadFile = Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;

            if (uploadFile != null)
            {
                var backupName = uploadFile.FileName;
                var validationResult = _db.DataProvider.ValidateBackupFileName(backupName);
                if (validationResult.IsValid)
                {
                    var dir = await Services.ApplicationContext.TenantRoot.GetDirectoryAsync(BACKUP_DIR);
                    var fs = dir.FileSystem;
                    var path = PathUtility.Join(dir.SubPath, backupName);

                    var targetFile = fs.CheckUniqueFileName(path, out var newPath)
                        ? await fs.GetFileAsync(newPath)
                        : await fs.GetFileAsync(path);

                    using var sourceStream = uploadFile.OpenReadStream();
                    using var targetStream = await targetFile.OpenWriteAsync();
                    await sourceStream.CopyToAsync(targetStream);

                    NotifyInfo(T("Admin.System.Maintenance.DbBackup.BackupUploaded"));
                }
                else
                {
                    NotifyError(T("Admin.System.Maintenance.DbBackup.InvalidBackup", backupName.NaIfEmpty()));
                }
            }
            else
            {
                NotifyError(T("Admin.Common.UploadFile"));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> RestoreBackup(string name)
        {
            if (PathUtility.HasInvalidFileNameChars(name))
            {
                throw new BadHttpRequestException("Invalid file name: " + name.NaIfEmpty());
            }

            try
            {
                if (_db.DataProvider.CanRestore)
                {
                    var dir = await Services.ApplicationContext.TenantRoot.GetDirectoryAsync(BACKUP_DIR);
                    var fs = dir.FileSystem;
                    var fullPath = fs.MapPath(PathUtility.Join(dir.SubPath, name));

                    await _db.DataProvider.RestoreDatabaseAsync(fullPath);

                    await ClearCache();
                    await ClearDatabaseCache();

                    NotifyInfo(T("Admin.System.Maintenance.DbBackup.DatabaseRestored"));
                }
                else
                {
                    NotifyError(T("Admin.System.Maintenance.DbBackup.RestoreNotSupported", _db.DataProvider.ProviderType.ToString().NaIfEmpty()));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> DeleteBackup(GridSelection selection)
        {
            var numDeleted = 0;
            var root = Services.ApplicationContext.TenantRoot;

            foreach (var fileName in selection.SelectedKeys)
            {
                if (await root.TryDeleteFileAsync(BACKUP_DIR + "\\" + fileName))
                {
                    numDeleted++;
                }
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> DownloadBackup(string name)
        {
            if (PathUtility.HasInvalidFileNameChars(name))
            {
                throw new BadHttpRequestException("Invalid file name: " + name.NaIfEmpty());
            }

            var root = Services.ApplicationContext.TenantRoot;
            var backup = await root.GetFileAsync(BACKUP_DIR + "\\" + name);
            var contentType = MimeTypes.MapNameToMimeType(backup.PhysicalPath);

            try
            {
                return new FileStreamResult(backup.OpenRead(), contentType)
                {
                    FileDownloadName = name
                };
            }
            catch (IOException)
            {
                NotifyWarning(T("Admin.Common.FileInUse"));
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region Utils

        /// <summary>
        /// Counts the size of all objects in both IMemoryCache and Smartstore memory cache
        /// </summary>
        private IDictionary<string, long> GetMemoryCacheStats()
        {
            var cache = Services.CacheFactory.GetMemoryCache();
            var stats = new Dictionary<string, long>();
            var instanceLookups = new HashSet<object>(ReferenceEqualityComparer.Instance) { cache, _memCache };

            // IMemoryCache
            var memCacheKeys = _memCache.EnumerateKeys().ToArray();
            foreach (var key in memCacheKeys)
            {
                var value = _memCache.Get(key);
                var size = GetObjectSize(value);

                if (key is string str)
                {
                    stats.Add("MemoryCache:" + str.Replace(':', '_'), size + (sizeof(char) + (str.Length + 1)));
                }
                else
                {
                    stats.Add("MemoryCache:" + key.ToString(), size + GetObjectSize(key));
                }
            }

            // Smartstore CacheManager
            var cacheKeys = cache.Keys("*").ToArray();
            foreach (var key in cacheKeys)
            {
                var value = cache.Get<object>(key);
                var size = GetObjectSize(value);

                stats.Add(key, size + (sizeof(char) + (key.Length + 1)));
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
