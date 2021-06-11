using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Export;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.DataExchange.Export.Deployment;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Utilities;
using Smartstore.Web.Controllers;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class ExportController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IExportProfileService _exportProfileService;
        private readonly IProviderManager _providerManager;
        private readonly ITaskStore _taskStore;
        private readonly DataExchangeSettings _dataExchangeSettings;

        public ExportController(
            SmartDbContext db,
            IExportProfileService exportProfileService,
            IProviderManager providerManager,
            ITaskStore taskStore,
            DataExchangeSettings dataExchangeSettings)
        {
            _db = db;
            _exportProfileService = exportProfileService;
            _providerManager = providerManager;
            _taskStore = taskStore;
            _dataExchangeSettings = dataExchangeSettings;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> List()
        {
            var model = new List<ExportProfileModel>();

            var providers = _exportProfileService.LoadAllExportProviders(0, false)
                .ToDictionarySafe(x => x.Metadata.SystemName);

            var profiles = await _db.ExportProfiles
                .AsNoTracking()
                .Include(x => x.Task)
                .Include(x => x.Deployments)
                .OrderBy(x => x.IsSystemProfile).ThenBy(x => x.Name)
                .ToListAsync();

            var lastExecutionInfos = (await _taskStore.GetExecutionInfoQuery(false)
                .ApplyCurrentMachineNameFilter()
                .ApplyTaskFilter(0, true)
                .ToListAsync())
                .ToDictionarySafe(x => x.TaskDescriptorId);

            foreach (var profile in profiles)
            {
                if (providers.TryGetValue(profile.ProviderSystemName, out var provider))
                {
                    var profileModel = new ExportProfileModel();

                    lastExecutionInfos.TryGetValue(profile.TaskId, out var lastExecutionInfo);
                    await PrepareProfileModel(profileModel, profile, provider, lastExecutionInfo, false);

                    var fileDetailsModel = await CreateFileDetailsModel(profile, null);
                    profileModel.FileCount = fileDetailsModel.FileCount;

                    // TODO: (mg) (core) add Task model.

                    model.Add(profileModel);
                }
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> ProfileFileDetails(int profileId, int deploymentId)
        {
            var model = await CreateFileDetailsModel(profileId, deploymentId);

            return model != null ? PartialView(model) : new EmptyResult();
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> ProfileFileCount(int profileId)
        {
            var model = await CreateFileDetailsModel(profileId, 0);

            return model != null ? PartialView(model.FileCount) : new EmptyResult();
        }

        [Permission(Permissions.Configuration.Export.Create)]
        public async Task<IActionResult> Create()
        {
            var num = 0;
            var providers = _exportProfileService.LoadAllExportProviders(0, false)
                .ToDictionarySafe(x => x.Metadata.SystemName);

            var profiles = await _db.ExportProfiles
                .AsNoTracking()
                .ApplyStandardFilter()
                .ToListAsync();

            var model = new ExportProfileModel();

            ViewBag.Providers = providers.Values
                .Select(x => new ExportProfileModel.ProviderSelectItem
                {
                    Id = ++num,
                    SystemName = x.Metadata.SystemName,
                    ImageUrl = GetThumbnailUrl(x),
                    // TODO: (mg) (core) PluginMediator required in ExportController.
                    //FriendlyName = _pluginMediator.GetLocalizedFriendlyName(x.Metadata),
                    FriendlyName = x.Metadata.SystemName,
                    //Description = _pluginMediator.GetLocalizedDescription(x.Metadata)
                    Description = x.Metadata.SystemName
                })
                .ToList();

            ViewBag.Profiles = profiles
                .Select(x => new ExportProfileModel.ProviderSelectItem
                {
                    Id = x.Id,
                    SystemName = x.ProviderSystemName,
                    FriendlyName = x.Name,
                    ImageUrl = GetThumbnailUrl(providers.Get(x.ProviderSystemName))
                })
                .ToList();

            return PartialView(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Export.Create)]
        public async Task<IActionResult> Create(ExportProfileModel model)
        {
            if (model.ProviderSystemName.HasValue())
            {
                var provider = _providerManager.GetProvider<IExportProvider>(model.ProviderSystemName);
                if (provider != null)
                {
                    var profile = await _exportProfileService.InsertExportProfileAsync(provider, false, null, model.CloneProfileId ?? 0);

                    return RedirectToAction("Edit", new { id = profile.Id });
                }
            }

            NotifyError(T("Admin.Common.ProviderNotLoaded", model.ProviderSystemName.NaIfEmpty()));

            return RedirectToAction("List");
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var profile = await _db.ExportProfiles
                .AsNoTracking()
                .ApplyStandardFilter()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (profile == null)
            {
                return RedirectToAction("List");
            }

            var provider = _providerManager.GetProvider<IExportProvider>(profile.ProviderSystemName);
            if (provider == null || provider.Metadata.IsHidden)
            {
                return RedirectToAction("List");
            }

            var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(profile.TaskId);

            var model = new ExportProfileModel();
            await PrepareProfileModel(model, profile, provider, lastExecutionInfo, true);

            return View(model);
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> ResolveFileNamePatternExample(int id, string pattern)
        {
            var profile = await _db.ExportProfiles.FindByIdAsync(id, false);

            var resolvedPattern = profile.ResolveFileNamePattern(
                Services.StoreContext.CurrentStore,
                1,
                _dataExchangeSettings.MaxFileNameLength,
                pattern.EmptyNull());

            return Content(resolvedPattern);
        }

        #region Utilities

        private async Task PrepareProfileModel(
            ExportProfileModel model, 
            ExportProfile profile, 
            Provider<IExportProvider> provider,
            TaskExecutionInfo lastExecutionInfo,
            bool createForEdit)
        {
            MiniMapper.Map(profile, model);

            var dir = await _exportProfileService.GetExportDirectoryAsync(profile, null, false);
            var logFile = await dir.GetFileAsync("log.txt");
            //var moduleDescriptor = provider.Metadata.ModuleDescriptor;

            model.TaskName = profile.Task.Name.NaIfEmpty();
            model.IsTaskRunning = lastExecutionInfo?.IsRunning ?? false;
            model.IsTaskEnabled = profile.Task.Enabled;
            model.LogFileExists = logFile.Exists;
            model.HasActiveProvider = provider != null;
            model.FileNamePatternDescriptions = T("Admin.DataExchange.Export.FileNamePatternDescriptions").Value.SplitSafe(";").ToArray();

            model.Provider = new ExportProfileModel.ProviderModel
            {
                EntityType = provider.Value.EntityType,
                EntityTypeName = await Services.Localization.GetLocalizedEnumAsync(provider.Value.EntityType),
                FileExtension = provider.Value.FileExtension,
                ThumbnailUrl = GetThumbnailUrl(provider),
                // TODO: (mg) (core) PluginMediator required in ExportController.
                //FriendlyName = _pluginMediator.GetLocalizedFriendlyName(provider.Metadata),
                FriendlyName = provider.Metadata.SystemName,
                //Description = _pluginMediator.GetLocalizedDescription(provider.Metadata),
                Description = provider.Metadata.SystemName,
                //Url = descriptor?.Url,
                //Author = descriptor?.Author,
                //Version = descriptor?.Version?.ToString()
            };

            if (!createForEdit)
            {
                return;
            }

            var projection = XmlHelper.Deserialize<ExportProjection>(profile.Projection);
            var filter = XmlHelper.Deserialize<ExportFilter>(profile.Filtering);

            var language = Services.WorkContext.WorkingLanguage;
            var store = Services.StoreContext.CurrentStore;
            var stores = Services.StoreContext.GetAllStores();
            var emailAccounts = await _db.EmailAccounts.AsNoTracking().ToListAsync();

            var languages = await _db.Languages
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var currencies = await _db.Currencies
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            model.StoreCount = stores.Count;
            model.Offset = profile.Offset;
            model.Limit = profile.Limit == 0 ? null : profile.Limit;
            model.BatchSize = profile.BatchSize == 0 ? null : profile.BatchSize;
            model.PerStore = profile.PerStore;
            model.EmailAccountId = profile.EmailAccountId;
            model.CompletedEmailAddresses = profile.CompletedEmailAddresses.SplitSafe(",").ToArray();
            model.CreateZipArchive = profile.CreateZipArchive;
            model.Cleanup = profile.Cleanup;
            model.PrimaryStoreCurrencyCode = Services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;
            model.FileNamePatternExample = profile.ResolveFileNamePattern(store, 1, _dataExchangeSettings.MaxFileNameLength);

            ViewBag.EmailAccounts = emailAccounts
                .Select(x => new SelectListItem { Text = x.FriendlyName, Value = x.Id.ToString() })
                .ToList();

            ViewBag.CompletedEmailAddresses = new MultiSelectList(profile.CompletedEmailAddresses.SplitSafe(","));

            // TODO: (mg) (core) check\test whether such ViewBag objects can really be used multiple times for different view controls.
            ViewBag.Stores = stores
                .Select(y => new SelectListItem { Text = y.Name, Value = y.Id.ToString() })
                .ToList();

            ViewBag.Languages = languages
                .Select(y => new SelectListItem { Text = y.Name, Value = y.Id.ToString() })
                .ToList();

            ViewBag.Currencies = currencies
                .Select(y => new SelectListItem { Text = y.Name, Value = y.Id.ToString() })
                .ToList();

            // Projection.
            model.Projection = MiniMapper.Map<ExportProjection, ExportProjectionModel>(projection);
            model.Projection.NumberOfPictures = projection.NumberOfMediaFiles;
            model.Projection.AppendDescriptionText = projection.AppendDescriptionText.SplitSafe(",").ToArray();
            model.Projection.CriticalCharacters = projection.CriticalCharacters.SplitSafe(",").ToArray();

            if (profile.Projection.IsEmpty())
            {
                model.Projection.DescriptionMergingId = (int)ExportDescriptionMerging.Description;
            }

            // Filtering.
            model.Filter = MiniMapper.Map<ExportFilter, ExportFilterModel>(filter);
            // TODO: (mg) (core) 'Common.Unspecified' required for export filter languages.

            // Deployment.
            model.Deployments = await profile.Deployments.SelectAsync(async x =>
            {
                var deploymentModel = await CreateDeploymentModel(profile, x, null, false);

                if (x.ResultInfo.HasValue())
                {
                    var resultInfo = XmlHelper.Deserialize<DataDeploymentResult>(x.ResultInfo);
                    var lastExecution = Services.DateTimeHelper.ConvertToUserTime(resultInfo.LastExecutionUtc, DateTimeKind.Utc);

                    deploymentModel.LastResult = new ExportDeploymentModel.LastResultInfo
                    {
                        Execution = lastExecution,
                        ExecutionPretty = lastExecution.Humanize(false),
                        Error = resultInfo.LastError
                    };
                }

                return deploymentModel;
            })
            .ToListAsync();

            //...
        }

        private async Task<ExportDeploymentModel> CreateDeploymentModel(
            ExportProfile profile,
            ExportDeployment deployment,
            Provider<IExportProvider> provider,
            bool forEdit)
        {
            var model = MiniMapper.Map<ExportDeployment, ExportDeploymentModel>(deployment);

            model.EmailAddresses = deployment.EmailAddresses.SplitSafe(",").ToArray();
            model.DeploymentTypeName = await Services.Localization.GetLocalizedEnumAsync(deployment.DeploymentType);
            model.PublicFolderUrl = await _exportProfileService.GetDeploymentDirectoryUrlAsync(deployment);

            if (forEdit)
            {
                model.CreateZip = profile.CreateZipArchive;
                
                ViewBag.DeploymentTypes = ExportDeploymentType.FileSystem.ToSelectList(false).ToList();
                ViewBag.HttpTransmissionTypes = ExportHttpTransmissionType.SimplePost.ToSelectList(false).ToList();

                if (provider != null)
                {
                    model.ThumbnailUrl = GetThumbnailUrl(provider);
                }
            }
            else
            {
                model.FileCount = (await CreateFileDetailsModel(profile, deployment)).FileCount;
            }

            return model;
        }

        private async Task<ExportFileDetailsModel> CreateFileDetailsModel(int profileId, int deploymentId)
        {
            if (profileId != 0)
            {
                var profile = await _db.ExportProfiles
                    .AsNoTracking()
                    .ApplyStandardFilter()
                    .FirstOrDefaultAsync(x => x.Id == profileId);

                if (profile != null)
                {
                    var provider = _providerManager.GetProvider<IExportProvider>(profile.ProviderSystemName);
                    if (provider != null && !provider.Metadata.IsHidden)
                    {
                        return await CreateFileDetailsModel(profile, null);
                    }
                }
            }
            else if (deploymentId != 0)
            {
                var deployment = await _db.ExportDeployments
                    .AsNoTracking()
                    .Include(x => x.Profile)
                    .FirstOrDefaultAsync(x => x.Id == deploymentId);

                if (deployment != null)
                {
                    return await CreateFileDetailsModel(deployment.Profile, deployment);
                }
            }

            return null;
        }

        private async Task<ExportFileDetailsModel> CreateFileDetailsModel(ExportProfile profile, ExportDeployment deployment)
        {
            var model = new ExportFileDetailsModel
            {
                Id = deployment?.Id ?? profile.Id,
                IsForDeployment = deployment != null
            };

            try
            {
                var rootPath = (await Services.ApplicationContext.ContentRoot.GetDirectoryAsync(null)).PhysicalPath;

                // Add export files.
                var dir = await _exportProfileService.GetExportDirectoryAsync(profile, "Content", false);
                var zipFile = await dir.Parent.GetFileAsync(dir.Parent.Name.ToValidFileName() + ".zip");
                var resultInfo = XmlHelper.Deserialize<DataExportResult>(profile.ResultInfo);

                if (deployment == null)
                {
                    await AddFileInfo(model.ExportFiles, zipFile, rootPath);

                    if (resultInfo.Files != null)
                    {
                        foreach (var fi in resultInfo.Files)
                        {
                            await AddFileInfo(model.ExportFiles, await dir.GetFileAsync(fi.FileName), rootPath, null, fi);
                        }
                    }
                }
                else if (deployment.DeploymentType == ExportDeploymentType.FileSystem)
                {
                    if (resultInfo.Files != null)
                    {
                        var deploymentDir = await _exportProfileService.GetDeploymentDirectoryAsync(deployment);
                        if (deploymentDir != null)
                        {
                            foreach (var fi in resultInfo.Files)
                            {
                                await AddFileInfo(model.ExportFiles, await deploymentDir.GetFileAsync(fi.FileName), rootPath, null, fi);
                            }
                        }
                    }
                }

                // Add public files.
                var publicDeployment = deployment == null
                    ? profile.Deployments.FirstOrDefault(x => x.DeploymentType == ExportDeploymentType.PublicFolder)
                    : (deployment.DeploymentType == ExportDeploymentType.PublicFolder ? deployment : null);

                if (publicDeployment != null)
                {
                    var currentStore = Services.StoreContext.CurrentStore;
                    var deploymentDir = await _exportProfileService.GetDeploymentDirectoryAsync(publicDeployment);
                    if (deploymentDir != null)
                    {
                        // INFO: public folder is not cleaned up during export. We only have to show files that has been created during last export.
                        // Otherwise the merchant might publish URLs of old export files.
                        if (profile.CreateZipArchive)
                        {                          
                            var url = await _exportProfileService.GetDeploymentDirectoryUrlAsync(publicDeployment, currentStore);
                            await AddFileInfo(model.PublicFiles, await deploymentDir.GetFileAsync(zipFile.Name), rootPath, url);
                        }
                        else if (resultInfo.Files != null)
                        {
                            var stores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
                            foreach (var fi in resultInfo.Files)
                            {
                                stores.TryGetValue(fi.StoreId, out var store);
                                
                                var url = await _exportProfileService.GetDeploymentDirectoryUrlAsync(publicDeployment, store ?? currentStore);
                                await AddFileInfo(model.PublicFiles, await deploymentDir.GetFileAsync(fi.FileName), rootPath, url, fi, store);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return model;
        }

        private async Task AddFileInfo(
            List<ExportFileDetailsModel.FileInfo> fileInfos,
            IFile file,
            string rootPath,
            string publicUrl = null,
            DataExportResult.ExportFileInfo fileInfo = null,
            Store store = null)
        {
            if (!(file?.Exists ?? false))
                return;

            if (fileInfos.Any(x => x.File.Name == file.Name))
                return;

            var fi = new ExportFileDetailsModel.FileInfo
            {
                File = file,
                DisplayOrder = file.Extension.EqualsNoCase(".zip") ? 0 : 1,
                FileRootPath = file.PhysicalPath.Replace(rootPath, "~/").Replace('\\', '/')
            };

            if (fileInfo != null)
            {
                fi.RelatedType = fileInfo.RelatedType;

                if (fileInfo.Label.HasValue())
                {
                    fi.Label = fileInfo.Label;
                }
                else
                {
                    fi.Label = T("Admin.Common.Data");

                    if (fileInfo.RelatedType.HasValue)
                    {
                        fi.Label += " " + await Services.Localization.GetLocalizedEnumAsync(fileInfo.RelatedType.Value);
                    }
                }
            }

            if (store != null)
            {
                fi.StoreId = store.Id;
                fi.StoreName = store.Name;
            }

            if (publicUrl.HasValue())
            {
                fi.FileUrl = publicUrl + fi.File.Name;
            }

            fileInfos.Add(fi);
        }

        private string GetThumbnailUrl(Provider<IExportProvider> provider)
        {
            string url = null;

            // TODO: (mg) (core) PluginMediator required in ExportController.
            url = "http://demo.smartstore.com/backend/Administration/Content/images/icon-plugin-default.png";
            //if (provider != null)
            //    url = _pluginMediator.GetIconUrl(provider.Metadata);

            //if (url.IsEmpty())
            //    url = _pluginMediator.GetDefaultIconUrl(null);

            //url = Url.Content(url);

            return url;
        }

        #endregion
    }
}
