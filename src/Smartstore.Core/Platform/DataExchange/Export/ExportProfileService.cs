using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class ExportProfileService : AsyncDbSaveHook<ExportProfile>, IExportProfileService
    {
        private const string FILE_NAME_PATTERN = "%Store.Id%-%Profile.Id%-%File.Index%-%Profile.SeoName%";
        private const string EXPORT_FILE_ROOT = "ExportProfiles";

        private static readonly Regex _regexFolderName = new(".*/ExportProfiles/?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private readonly SmartDbContext _db;
        private readonly IApplicationContext _appContext;
        private readonly IStoreContext _storeContext;
        private readonly ILocalizationService _localizationService;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly ITaskStore _taskStore;
        private readonly IProviderManager _providerManager;
        private readonly DataExchangeSettings _dataExchangeSettings;

        public ExportProfileService(
            SmartDbContext db,
            IApplicationContext appContext,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            Lazy<IUrlHelper> urlHelper,
            ITaskStore taskStore,
            IProviderManager providerManager,
            DataExchangeSettings dataExchangeSettings)
        {
            _db = db;
            _appContext = appContext;
            _storeContext = storeContext;
            _localizationService = localizationService;
            _urlHelper = urlHelper;
            _taskStore = taskStore;
            _providerManager = providerManager;
            _dataExchangeSettings = dataExchangeSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        #region Hook

        protected override Task<HookResult> OnUpdatingAsync(ExportProfile entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // INFO: validation of 'FolderName' not necessary anymore. Contains only the name of the export folder (no more path information).
            if (entity.FolderName.HasValue() && entity.FolderName[0] == '~')
            {
                // Map legacy folder names. Examples:
                // ~/App_Data/ExportProfiles/smartstorecategorycsv
                // ~/App_Data/Tenants/Default/ExportProfiles/smartstoreshoppingcartitemcsv
                var newFolderName = _regexFolderName.Replace(PathUtility.NormalizeRelativePath(entity.FolderName).TrimEnd('/'), string.Empty);

                if (newFolderName.IsEmpty())
                {
                    // Profile folder is root folder '~/App_Data/ExportProfiles/'.
                    var cleanedProviderName = entity.ProviderSystemName
                        .Replace("Exports.", string.Empty)
                        .Replace("Feeds.", string.Empty)
                        .Replace("/", string.Empty)
                        .Replace("-", string.Empty);

                    var folderName = SlugUtility.Slugify(cleanedProviderName, true, false, false)
                        .Truncate(_dataExchangeSettings.MaxFileNameLength);

                    newFolderName = _appContext.TenantRoot.CreateUniqueDirectoryName(EXPORT_FILE_ROOT, folderName);
                }

                if (newFolderName.HasValue())
                {
                    entity.FolderName = newFolderName;
                }
            }

            return Task.FromResult(HookResult.Ok);
        }

        #endregion

        public virtual async Task<IDirectory> GetExportDirectoryAsync(ExportProfile profile, string subpath = null, bool createIfNotExists = false)
        {
            Guard.NotNull(profile, nameof(profile));
            Guard.IsTrue(profile.FolderName.EmptyNull().Length > 2, nameof(profile.FolderName), "The export folder name must be at least 3 characters long.");

            // Legacy examples:
            // ~/App_Data/ExportProfiles/smartstorecategorycsv
            // ~/App_Data/Tenants/Default/ExportProfiles/smartstoreshoppingcartitemcsv
            var root = _appContext.TenantRoot;
            var path = PathUtility.Join(EXPORT_FILE_ROOT, _regexFolderName.Replace(profile.FolderName, string.Empty), subpath.EmptyNull());
            var dir = await root.GetDirectoryAsync(path);

            if (createIfNotExists)
            {
                await dir.CreateAsync();
            }

            return dir;
        }

        public virtual async Task<IDirectory> GetDeploymentDirectoryAsync(ExportDeployment deployment, bool createIfNotExists = false)
        {
            if (deployment != null)
            {
                if (deployment.DeploymentType == ExportDeploymentType.PublicFolder)
                {
                    var webRoot = _appContext.WebRoot;
                    var path = PathUtility.Join(DataExporter.PublicDirectoryName, deployment.SubFolder);
                    var dir = await webRoot.GetDirectoryAsync(path);

                    if (createIfNotExists)
                    {
                        await dir.CreateAsync();
                    }

                    return dir;
                }
                else if (deployment.DeploymentType == ExportDeploymentType.FileSystem && deployment.FileSystemPath.HasValue())
                {
                    // Any file system path is allowed.
                    var fullPath = deployment.FileSystemPath;

                    if (!PathUtility.IsAbsolutePhysicalPath(fullPath))
                    {
                        fullPath = CommonHelper.MapPath(PathUtility.NormalizeRelativePath(fullPath));
                    }

                    if (!Directory.Exists(fullPath))
                    {
                        if (!createIfNotExists)
                        {
                            return null;
                        }

                        try
                        {
                            Directory.CreateDirectory(fullPath);
                        }
                        catch
                        {
                            return null;
                        }
                    }

                    // 'fullPath' must exist for LocalFileSystem (otherwise exception)!
                    var root = new LocalFileSystem(fullPath);

                    return await root.GetDirectoryAsync(null);
                }
            }

            return null;
        }

        public virtual async Task<string> GetDeploymentDirectoryUrlAsync(ExportDeployment deployment, Store store = null)
        {
            if (deployment != null && deployment.DeploymentType == ExportDeploymentType.PublicFolder)
            {
                if (store == null)
                {
                    await _db.LoadReferenceAsync(deployment, x => x.Profile);

                    var filter = XmlHelper.Deserialize<ExportFilter>(deployment.Profile.Filtering);
                    var storeId = filter.StoreId;

                    if (storeId == 0)
                    {
                        var projection = XmlHelper.Deserialize<ExportProjection>(deployment.Profile.Projection);
                        storeId = projection.StoreId ?? 0;
                    }

                    store = _storeContext.GetStoreById(storeId) ?? _storeContext.CurrentStore;
                }

                // Always use IUrlHelper.Content("~/subpath") or WebHelper.ToAbsolutePath("~/subpath") for public URLs,
                // so that IIS application path can be prepended if applicable. 
                var path = WebHelper.ToAppRelativePath(PathUtility.Join(DataExporter.PublicDirectoryName, deployment.SubFolder));

                return store.Url.TrimEnd('/') + _urlHelper.Value.Content(path).EnsureEndsWith("/");
            }

            return null;
        }

        public virtual async Task<ExportProfile> InsertExportProfileAsync(
            Provider<IExportProvider> provider,
            bool isSystemProfile = false,
            string profileSystemName = null,
            int cloneFromProfileId = 0)
        {
            Guard.NotNull(provider, nameof(provider));

            var providerSystemName = provider.Metadata.SystemName;
            var resourceName = provider.Metadata.ResourceKeyPattern.FormatInvariant(providerSystemName, "FriendlyName");
            var profileName = await _localizationService.GetResourceAsync(resourceName, 0, false, providerSystemName, true);

            var profile = await InsertExportProfileAsync(
                providerSystemName,
                profileName.NullEmpty() ?? providerSystemName,
                provider.Value.FileExtension,
                provider.Metadata.ExportFeatures,
                isSystemProfile,
                profileSystemName,
                cloneFromProfileId);

            return profile;
        }

        public virtual async Task<ExportProfile> InsertExportProfileAsync(
            string providerSystemName,
            string name,
            string fileExtension,
            ExportFeatures features,
            bool isSystemProfile = false,
            string profileSystemName = null,
            int cloneFromProfileId = 0)
        {
            Guard.NotEmpty(providerSystemName, nameof(providerSystemName));

            if (name.IsEmpty())
            {
                name = providerSystemName;
            }

            if (!isSystemProfile)
            {
                var profileCount = await _db.ExportProfiles.CountAsync(x => x.ProviderSystemName == providerSystemName);
                name = $"{T("Common.My").Value} {name} {profileCount + 1}";
            }

            TaskDescriptor task = null;
            ExportProfile cloneProfile = null;
            ExportProfile profile = null;

            if (cloneFromProfileId != 0)
            {
                cloneProfile = await _db.ExportProfiles
                    .Include(x => x.Task)
                    .Include(x => x.Deployments)
                    .FindByIdAsync(cloneFromProfileId);
            }

            if (cloneProfile == null)
            {
                task = _taskStore.CreateDescriptor(name + " Task", typeof(DataExportTask));
                task.Enabled = false;
                task.CronExpression = "0 */6 * * *"; // Every six hours.
                task.StopOnError = false;
                task.IsHidden = true;
            }
            else
            {
                task = cloneProfile.Task.Clone();
                task.Name = name + " Task";
            }

            await _taskStore.InsertTaskAsync(task);

            if (cloneProfile == null)
            {
                profile = new ExportProfile
                {
                    FileNamePattern = FILE_NAME_PATTERN
                };

                if (isSystemProfile)
                {
                    profile.Enabled = true;
                    profile.PerStore = false;
                    profile.CreateZipArchive = false;
                    profile.Cleanup = false;
                }
                else
                {
                    // What we do here is to preset typical settings for feed creation
                    // but on the other hand they may be untypical for generic data export\exchange.
                    var projection = new ExportProjection
                    {
                        RemoveCriticalCharacters = true,
                        CriticalCharacters = "¼,½,¾",
                        PriceType = PriceDisplayType.PreSelectedPrice,
                        NoGroupedProducts = features.HasFlag(ExportFeatures.CanOmitGroupedProducts),
                        OnlyIndividuallyVisibleAssociated = true,
                        DescriptionMerging = ExportDescriptionMerging.Description
                    };

                    var filter = new ExportFilter
                    {
                        IsPublished = true,
                        ShoppingCartTypeId = (int)ShoppingCartType.ShoppingCart
                    };

                    profile.Projection = XmlHelper.Serialize(projection);
                    profile.Filtering = XmlHelper.Serialize(filter);
                }
            }
            else
            {
                profile = cloneProfile.Clone();
            }

            profile.IsSystemProfile = isSystemProfile;
            profile.Name = name;
            profile.ProviderSystemName = providerSystemName;
            profile.TaskId = task.Id;

            var cleanedProviderName = providerSystemName
                .Replace("Exports.", string.Empty)
                .Replace("Feeds.", string.Empty)
                .Replace("/", string.Empty)
                .Replace("-", string.Empty);

            var folderName = SlugUtility.Slugify(cleanedProviderName, true, false, false)
                .Truncate(_dataExchangeSettings.MaxFileNameLength);

            profile.FolderName = _appContext.TenantRoot.CreateUniqueDirectoryName(EXPORT_FILE_ROOT, folderName);
            profile.SystemName = profileSystemName.IsEmpty() && isSystemProfile
                ? cleanedProviderName
                : profileSystemName;

            _db.ExportProfiles.Add(profile);

            // Get the export profile ID.
            await _db.SaveChangesAsync();

            task.Alias = profile.Id.ToString();

            if (fileExtension.HasValue() && !isSystemProfile)
            {
                if (cloneProfile == null)
                {
                    if (features.HasFlag(ExportFeatures.CreatesInitialPublicDeployment))
                    {
                        var webRoot = _appContext.WebRoot;
                        var subfolder = webRoot.CreateUniqueDirectoryName(DataExporter.PublicDirectoryName, folderName);
                        _ = await webRoot.TryCreateDirectoryAsync(PathUtility.Join(DataExporter.PublicDirectoryName, subfolder));

                        profile.Deployments.Add(new ExportDeployment
                        {
                            ProfileId = profile.Id,
                            Enabled = true,
                            DeploymentType = ExportDeploymentType.PublicFolder,
                            Name = profile.Name,
                            SubFolder = subfolder
                        });
                    }
                }
                else
                {
                    cloneProfile.Deployments.Each(x => profile.Deployments.Add(x.Clone()));
                }
            }

            // Finally update task and export profile.
            await _taskStore.UpdateTaskAsync(task);
            await _db.SaveChangesAsync();

            return profile;
        }

        public virtual async Task DeleteExportProfileAsync(ExportProfile profile, bool force = false)
        {
            if (profile == null)
            {
                return;
            }

            if (!force && profile.IsSystemProfile)
            {
                throw new InvalidOperationException(T("Admin.DataExchange.Export.CannotDeleteSystemProfile"));
            }

            await _db.LoadCollectionAsync(profile, x => x.Deployments);
            await _db.LoadReferenceAsync(profile, x => x.Task);

            var directory = await GetExportDirectoryAsync(profile);
            var deployments = profile.Deployments.Where(x => !x.IsTransientRecord()).ToList();

            if (profile.Deployments.Any())
            {
                _db.ExportDeployments.RemoveRange(deployments);
            }

            _db.ExportProfiles.Remove(profile);

            await _db.SaveChangesAsync();

            if (profile.Task != null)
            {
                await _taskStore.DeleteTaskAsync(profile.Task);
            }

            if (directory.Exists)
            {
                directory.FileSystem.ClearDirectory(directory, true, TimeSpan.Zero);
            }
        }

        public virtual IEnumerable<Provider<IExportProvider>> LoadAllExportProviders(int storeId = 0, bool includeHidden = true)
        {
            var allProviders = _providerManager.GetAllProviders<IExportProvider>(storeId)
                .Where(x => x.Value != null && (includeHidden || !x.Metadata.IsHidden))
                .OrderBy(x => x.Metadata.FriendlyName);

            return allProviders;
        }

        public virtual async Task<(int DeletedFiles, int DeletedFolders)> DeleteExportFilesAsync(DateTime? startDate, DateTime? endDate)
        {
            var numFiles = 0;
            var numFolders = 0;
            var webRoot = _appContext.WebRoot;
            var tenantRoot = _appContext.TenantRoot;
            var directories = new List<IDirectory>
            {
                await webRoot.GetDirectoryAsync(DataExporter.PublicDirectoryName),
                await tenantRoot.GetDirectoryAsync(EXPORT_FILE_ROOT)
            };

            foreach (var dir in directories.Where(x => x.Exists))
            {
                var files = dir.EnumerateFiles(deep: true);

                foreach (var file in files)
                {
                    if (!file.Name.EqualsNoCase("index.htm") && !file.Name.EqualsNoCase("placeholder"))
                    {
                        try
                        {
                            if ((!startDate.HasValue || startDate.Value < file.CreatedOn) &&
                                (!endDate.HasValue || file.CreatedOn < endDate.Value))
                            {
                                await file.DeleteAsync();
                                numFiles++;
                            }
                        }
                        catch
                        {
                            // Do nothing. We are just cleaning up.
                        }
                    }
                }

                foreach (var subdir in dir.EnumerateDirectories())
                {
                    if ((!startDate.HasValue || startDate.Value < subdir.LastModified) &&
                        (!endDate.HasValue || subdir.LastModified < endDate.Value))
                    {
                        dir.FileSystem.ClearDirectory(subdir, true, TimeSpan.Zero);
                        numFolders++;
                    }
                }
            }

            return (numFiles, numFolders);
        }
    }
}