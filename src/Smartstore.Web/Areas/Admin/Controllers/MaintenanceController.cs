using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Smartstore.Admin.Models.Maintenance;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Identity;
using Smartstore.Core.Logging;
using Smartstore.Core.Packaging;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Data.Caching;
using Smartstore.Data.Providers;
using Smartstore.Http;
using Smartstore.Imaging;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Threading;
using Smartstore.Utilities;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class MaintenanceController : AdminController
    {
        const string BackupDir = "DbBackups";

        private readonly SmartDbContext _db;
        private readonly IMemoryCache _memCache;
        private readonly ITaskScheduler _taskScheduler;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ICustomerService _customerService;
        private readonly IImageFactory _imageFactory;
        private readonly Lazy<IImageCache> _imageCache;
        private readonly Lazy<IImageOffloder> _imageOffloader;
        private readonly Lazy<IFilePermissionChecker> _filePermissionChecker;
        private readonly Lazy<ICurrencyService> _currencyService;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly Lazy<IShippingService> _shippingService;
        private readonly Lazy<IExportProfileService> _exportProfileService;
        private readonly Lazy<IImportProfileService> _importProfileService;
        private readonly Lazy<UpdateChecker> _updateChecker;
        private readonly MeasureSettings _measureSettings;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly AsyncRunner _asyncRunner;
        private readonly IMediaService _mediaService;
        
        public MaintenanceController(
            SmartDbContext db,
            IMemoryCache memCache,
            ITaskScheduler taskScheduler,
            IHttpClientFactory httpClientFactory,
            IHostApplicationLifetime hostApplicationLifetime,
            ICustomerService customerService,
            IImageFactory imageFactory,
            Lazy<IImageCache> imageCache,
            Lazy<IImageOffloder> imageOffloader,
            Lazy<IFilePermissionChecker> filePermissionChecker,
            Lazy<ICurrencyService> currencyService,
            Lazy<IPaymentService> paymentService,
            Lazy<IShippingService> shippingService,
            Lazy<IExportProfileService> exportProfileService,
            Lazy<IImportProfileService> importProfileService,
            Lazy<UpdateChecker> updateChecker,
            MeasureSettings measureSettings,
            IHostApplicationLifetime appLifetime,
            AsyncRunner asyncRunner,
            IMediaService mediaService)
        {
            _db = db;
            _memCache = memCache;
            _taskScheduler = taskScheduler;
            _httpClientFactory = httpClientFactory;
            _hostApplicationLifetime = hostApplicationLifetime;
            _customerService = customerService;
            _imageFactory = imageFactory;
            _imageCache = imageCache;
            _imageOffloader = imageOffloader;
            _filePermissionChecker = filePermissionChecker;
            _currencyService = currencyService;
            _paymentService = paymentService;
            _shippingService = shippingService;
            _exportProfileService = exportProfileService;
            _importProfileService = importProfileService;
            _updateChecker = updateChecker;
            _measureSettings = measureSettings;
            _appLifetime = appLifetime;
            _asyncRunner = asyncRunner;
            _mediaService = mediaService;
        }

        #region Maintenance

        [Permission(Permissions.System.Maintenance.Read)]
        public async Task<IActionResult> Index()
        {
            var model = new MaintenanceModel
            {
                CanExecuteSql = _db.DataProvider.CanExecuteSqlScript,
                CanCreateBackup = _db.DataProvider.CanBackup,
                SqlQuery = TempData["Maintenance.SqlQuery"].Convert<string>()
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dtHelper = Services.DateTimeHelper;

            DateTime? startDateValue = model.DeleteGuests.StartDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.DeleteGuests.StartDate.Value, dtHelper.CurrentTimeZone);

            DateTime? endDateValue = model.DeleteGuests.EndDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.DeleteGuests.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

            // Execute
            var numDeletedCustomers = await _customerService.DeleteGuestCustomersAsync(
                startDateValue,
                endDateValue,
                model.DeleteGuests.OnlyWithoutShoppingCart,
                _appLifetime.ApplicationStopping);

            NotifyInfo(T("Admin.System.Maintenance.DeleteGuests.TotalDeleted", numDeletedCustomers));

            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("delete-export-files")]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> DeleteExportFiles(MaintenanceModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

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
                TempData["Maintenance.SqlQuery"] = model.SqlQuery;

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

        [MaintenanceAction]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> OffloadEmbeddedImages(int take = 200)
        {
            //var result = await ProductPictureHelper.OffloadEmbeddedImages(_db, _mediaService.Value, take);
            var result = await _imageOffloader.Value.BatchOffloadEmbeddedImagesAsync(take);

            var message = result.ToString();

            if (result.NumAttempted < result.NumProcessedEntities)
            {
                message +=
                    Environment.NewLine +
                    Environment.NewLine +
                    "!! Apparently some embedded images could not be parsed and replaced correctly. Maybe incomplete or invalid HTML?";
            }

            if (result.NumProcessedEntities < result.NumAffectedEntities)
            {
                message +=
                    Environment.NewLine +
                    Environment.NewLine +
                    "Please re-execute this script to continue processing the rest of the entities.";
            }

            return Content(message);
        }

        [MaintenanceAction]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> ReInitMediaFileNames(string folderName = "")
        {
            var numProcessed = await _mediaService.EnsureMetadataResolvedAsync(folderName);
            return Content($"{numProcessed} files have been processed.");
        }

        [MaintenanceAction]
        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<string> RebuildTreePaths()
        {
            var numRebuilt = await CategoryService.RebuidTreePathsAsync(_db, _asyncRunner.AppShutdownCancellationToken);
            return T("Admin.System.Maintenance.TreePaths.PathCount", numRebuilt);
        }

        [MaintenanceAction]
        [Permission(Permissions.System.Maintenance.Execute)]
        public IActionResult CreateAttributeCombinationHashCodes()
        {
            _ = _asyncRunner.RunTask(CreateAttributeCombinationHashCodesInternal);

            NotifyInfo(T("Admin.System.ScheduleTasks.RunNow.Progress"));

            return RedirectToAction(nameof(Warnings));
        }

        private static async Task CreateAttributeCombinationHashCodesInternal(ILifetimeScope scope, CancellationToken cancelToken)
        {
            var db = scope.Resolve<SmartDbContext>();
            var logger = scope.Resolve<ILogger>();

            try
            {
                var migrator = new AttributesMigrator(db, logger);
                _ = await migrator.CreateAttributeCombinationHashCodesAsync(cancelToken);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            finally
            {
                scope.Resolve<IProductAttributeMaterializer>().ClearCachedAttributes();
            }
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
            return new StopApplicationResult();
        }

        /// <summary>
        /// It's a matter of order. Shutting down the application should be the last step
        /// in the request lifecycle.
        /// </summary>
        class StopApplicationResult : EmptyResult
        {
            public override async Task ExecuteResultAsync(ActionContext context)
            {
                var appLifetime = context.HttpContext.RequestServices.GetRequiredService<IHostApplicationLifetime>();
                await base.ExecuteResultAsync(context);
                appLifetime.StopApplication();
            }
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        [HttpPost]
        public async Task<IActionResult> ClearCache()
        {
            // Clear Smartstore inbuilt cache
            await Services.Cache.ClearAsync();

            // Clear IMemoryCache Smartstore: region
            _memCache.RemoveByPattern(_memCache.BuildScopedKey("*"));

            string message = T("Admin.Common.TaskSuccessfullyProcessed");
            NotifySuccess(message);

            return new JsonResult(new { Success = true, Message = message });
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

            string message = T("Admin.Common.TaskSuccessfullyProcessed");
            NotifySuccess(message);

            return new JsonResult(new { Success = true, Message = message });
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
                OperatingSystem = $"{runtimeInfo.OSDescription} ({runtimeInfo.ProcessArchitecture.ToString().ToLower()})",
                IPAddress = HttpContext.Connection.LocalIpAddress
            };

            // DB size
            if (dataProvider.CanComputeSize)
            {
                model.DatabaseSize = await CommonHelper.TryAction(dataProvider.GetDatabaseSizeAsync);
            }

            // DB table infos
            if (dataProvider.CanReadTableInfo)
            {
                model.DbTableInfos = await CommonHelper.TryAction(() => dataProvider.ReadTableInfosAsync(), []);
            }

            // Used RAM
            model.UsedMemorySize = CommonHelper.TryAction(GetPrivateBytes);

            // DB settings
            if (DataSettings.Instance.IsValid())
            {
                var allowExec = Services.Permissions.Authorize(Permissions.System.Maintenance.Execute);
                model.DataProviderFriendlyName = dataProvider.ProviderFriendlyName;
                model.OptimizeDatabaseEnabled = dataProvider.CanOptimizeDatabase && allowExec;
                model.OptimizeTableEnabled = dataProvider.CanOptimizeTable && allowExec;
            }

            // Loaded assemblies
            model.AppDate = CommonHelper.TryAction(() =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fi = new FileInfo(assembly.Location);
                return model.AppDate = fi.LastWriteTime.ToLocalTime();
            });

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var loadedAssembly = new SystemInfoModel.LoadedAssembly
                {
                    FullName = assembly.FullName
                };

                if (!assembly.IsDynamic)
                {
                    loadedAssembly.Location = CommonHelper.TryAction(() => assembly.Location);
                }

                model.LoadedAssemblies.Add(loadedAssembly);
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.Maintenance.Read)]
        public IActionResult UsedMemory()
        {
            try
            {
                var bytes = GetPrivateBytes();
                return Json(new { success = true, raw = bytes, pretty = Prettifier.HumanizeBytes(bytes) });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> GarbageCollect()
        {
            try
            {
                _imageFactory.ReleaseMemory();

                await Task.Delay(500);

                // Aggressive GC
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
                GC.WaitForPendingFinalizers();

                await Task.Delay(500);

                NotifySuccess(T("Admin.System.SystemInfo.GarbageCollectSuccessful"));
            }
            catch (Exception ex)
            {
                // Relaxed GC
                GC.Collect();
                GC.WaitForPendingFinalizers();

                NotifyError(ex);
            }

            return RedirectToReferrer();
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> OptimizeDatabase()
        {
            try
            {
                if (_db.DataProvider.CanOptimizeDatabase)
                {
                    await _db.DataProvider.OptimizeDatabaseAsync();
                    NotifySuccess(T("Common.ShrinkDatabaseSuccessful"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToReferrer();
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> OptimizeTable(string tableName, long? size = null)
        {
            try
            {
                if (_db.DataProvider.CanOptimizeTable)
                {
                    await _db.DataProvider.OptimizeTableAsync(tableName);
                    
                    var tableInfos = await CommonHelper.TryAction(() => _db.DataProvider.ReadTableInfosAsync(), []);
                    var currentSize = tableInfos.FirstOrDefault(x => x.TableName == tableName)?.TotalSpace;

                    if (size.HasValue && currentSize.HasValue && size > currentSize)
                    {
                        var diffBytes = currentSize.Value - size.Value;
                        var diffPercent = Math.Round(diffBytes / (double)currentSize, 2);

                        NotifySuccess(T("Common.OptimizeTableSuccess", 
                            tableName, 
                            Prettifier.HumanizeBytes(size.Value), 
                            Prettifier.HumanizeBytes(currentSize.Value),
                            Prettifier.HumanizeBytes(diffBytes),
                            "<b>" + diffPercent.ToString("P2") + "</b>"));
                    }
                    else
                    {
                        NotifyInfo(T("Common.OptimizeTableInfo", tableName));
                    }
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
            var storeUrl = store.GetBaseUrl();
            if (storeUrl.HasValue() && storeUrl.EqualsNoCase(Services.WebHelper.GetStoreLocation()))
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.URL.Match"));
            }
            else
            {
                AddEntry(SystemWarningLevel.Warning, T("Admin.System.Warnings.URL.NoMatch", storeUrl, Services.WebHelper.GetStoreLocation()));
            }

            // TaskScheduler reachability
            // ====================================
            try
            {
                using var taskSchedulerClient = await _taskScheduler.CreateHttpClientAsync();
                taskSchedulerClient.Timeout = TimeSpan.FromSeconds(5);

                using var response = await taskSchedulerClient.GetAsync("noop");
                response.EnsureSuccessStatusCode();

                var status = response.StatusCode;
                var warningModel = new SystemWarningModel
                {
                    Level = status == HttpStatusCode.OK ? SystemWarningLevel.Pass : SystemWarningLevel.Fail
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
                numActiveShippingMethods = _shippingService.Value.LoadEnabledShippingProviders()
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

            // Payment providers
            // ====================================
            int numEnabledPaymentProviders = 0;
            try
            {
                numEnabledPaymentProviders = (await _paymentService.Value.LoadAllPaymentProvidersAsync(onlyEnabled: true)).Count();
            }
            catch
            {
            }

            if (numEnabledPaymentProviders > 0)
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

            // Hash code of product attribute combinations
            // ====================================
            var missingHashCodes = await _db.ProductVariantAttributeCombinations.CountAsync(x => x.HashCode == 0);
            if (missingHashCodes > 0)
            {
                var msg = T("Admin.System.Warnings.AttributeCombinationHashCodes.Missing", missingHashCodes.ToString("N0"), Url.Action(nameof(CreateAttributeCombinationHashCodes)));
                AddEntry(SystemWarningLevel.Fail, msg);
            }
            else
            {
                AddEntry(SystemWarningLevel.Pass, T("Admin.System.Warnings.AttributeCombinationHashCodes.OK"));
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
            await root.TryCreateDirectoryAsync(BackupDir);

            var backups = await root
                .EnumerateFilesAsync(BackupDir)
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
                    var dir = await Services.ApplicationContext.TenantRoot.GetDirectoryAsync(BackupDir);
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
                    var dir = await Services.ApplicationContext.TenantRoot.GetDirectoryAsync(BackupDir);
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
                    var dir = await Services.ApplicationContext.TenantRoot.GetDirectoryAsync(BackupDir);
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
                var file = root.GetFile(PathUtility.Join(BackupDir, fileName));
                if (file.Exists)
                {
                    try
                    {
                        await file.DeleteAsync();
                        numDeleted++;
                    }
                    catch (Exception ex)
                    {
                        NotifyError(ex);
                    }
                }
            }

            return Json(new { Success = numDeleted > 0, Count = numDeleted });
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        public async Task<IActionResult> DownloadBackup(string name)
        {
            if (PathUtility.HasInvalidFileNameChars(name))
            {
                throw new BadHttpRequestException("Invalid file name: " + name.NaIfEmpty());
            }

            var root = Services.ApplicationContext.TenantRoot;
            var backup = await root.GetFileAsync(BackupDir + "\\" + name);
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

        ///// <summary>
        ///// Counts the byte size of all objects in both IMemoryCache and Smartstore memory cache
        ///// </summary>
        //private long GetMemCacheBytes()
        //{
        //    // System memory cache
        //    var size = 0L; // GetObjectSize(_memCache);

        //    // Smartstore memory cache
        //    var cache = Services.CacheFactory.GetMemoryCache();
        //    size += GetObjectSize(cache);

        //    return size;

        //    static long GetObjectSize(object obj)
        //    {
        //        if (obj == null)
        //        {
        //            return 0;
        //        }

        //        try
        //        {
        //            return CommonHelper.CalculateObjectSizeInBytes(obj);
        //        }
        //        catch
        //        {
        //            return 0;
        //        }
        //    }
        //}

        private static long GetPrivateBytes()
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();

            return process.PrivateMemorySize64;
        }

        #endregion
    }
}
