using System.Diagnostics;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.IO;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class ImportProfileService : IImportProfileService
    {
        const string ImportFileRoot = "ImportProfiles";

        private static readonly object _lock = new();
        private static Dictionary<ImportEntityType, Dictionary<string, string>> _entityProperties = null;

        private readonly SmartDbContext _db;
        private readonly IApplicationContext _appContext;
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;
        private readonly DataExchangeSettings _dataExchangeSettings;
        private readonly ITaskStore _taskStore;

        public ImportProfileService(
            SmartDbContext db,
            IApplicationContext appContext,
            ILocalizationService localizationService,
            ILanguageService languageService,
            DataExchangeSettings dataExchangeSettings,
            ITaskStore taskStore)
        {
            _db = db;
            _appContext = appContext;
            _localizationService = localizationService;
            _languageService = languageService;
            _dataExchangeSettings = dataExchangeSettings;
            _taskStore = taskStore;
        }

        public virtual async Task<IDirectory> GetImportDirectoryAsync(ImportProfile profile, string path = null, bool createIfNotExists = false)
        {
            Guard.NotNull(profile);

            if (PathUtility.IsAbsolutePhysicalPath(path))
            {
                if (createIfNotExists && !Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return new LocalFileSystem(path).GetDirectory(null);
            }
            else
            {
                var root = _appContext.TenantRoot;
                var fullPath = PathUtility.Join(ImportFileRoot, profile.FolderName, path.EmptyNull());
                var dir = await root.GetDirectoryAsync(fullPath);

                if (createIfNotExists)
                {
                    await dir.CreateAsync();
                }

                return dir;
            }
        }

        public virtual async Task<IList<ImportFile>> GetImportFilesAsync(ImportProfile profile, bool includeRelatedFiles = true)
        {
            var result = new List<ImportFile>();
            var directory = await GetImportDirectoryAsync(profile, "Content");

            if (directory.Exists)
            {
                var files = directory.EnumerateFiles();
                if (files.Any())
                {
                    foreach (var file in files)
                    {
                        var importFile = new ImportFile(file);
                        if (!includeRelatedFiles && importFile.RelatedType.HasValue)
                        {
                            continue;
                        }

                        result.Add(importFile);
                    }

                    // Always main data files first.
                    result = result
                        .OrderBy(x => x.RelatedType)
                        .ThenBy(x => x.File.Name)
                        .ToList();
                }
            }

            return result;
        }

        public virtual async Task<string> GetNewProfileNameAsync(ImportEntityType entityType)
        {
            var defaultNamesStr = _localizationService.GetResource("Admin.DataExchange.Import.DefaultProfileNames");
            var defaultNames = defaultNamesStr.SplitSafe(';').ToArray();
            var profileCount = 1 + await _db.ImportProfiles.CountAsync(x => x.EntityTypeId == (int)entityType);

            var result = defaultNames.ElementAtOrDefault((int)entityType).NullEmpty() ?? entityType.ToString();
            return result + " " + profileCount.ToString();
        }

        public virtual async Task<ImportProfile> InsertImportProfileAsync(string fileName, string name, ImportEntityType entityType)
        {
            Guard.NotEmpty(fileName);

            if (name.IsEmpty())
            {
                name = await GetNewProfileNameAsync(entityType);
            }

            var task = _taskStore.CreateDescriptor(name + " Task", typeof(DataImportTask));
            task.Enabled = false;
            task.CronExpression = "0 */24 * * *"; // Every 24 hours
            task.StopOnError = false;
            task.IsHidden = true;

            await _taskStore.InsertTaskAsync(task);

            var profile = new ImportProfile
            {
                Name = name,
                EntityType = entityType,
                Enabled = true,
                TaskId = task.Id,
                FileType = Path.GetExtension(fileName).EqualsNoCase(".xlsx")
                    ? ImportFileType.Xlsx
                    : ImportFileType.Csv
            };

            switch (entityType)
            {
                case ImportEntityType.Product:
                    profile.KeyFieldNames = string.Join(',', ProductImporter.DefaultKeyFields);
                    break;
                case ImportEntityType.Category:
                    profile.KeyFieldNames = string.Join(',', CategoryImporter.DefaultKeyFields);
                    break;
                case ImportEntityType.Customer:
                    profile.KeyFieldNames = string.Join(',', CustomerImporter.DefaultKeyFields);
                    break;
                case ImportEntityType.NewsletterSubscription:
                    profile.KeyFieldNames = string.Join(',', NewsletterSubscriptionImporter.DefaultKeyFields);
                    break;
            }

            var folderName = SlugUtility.Slugify(name, true, false, false)
                .Truncate(_dataExchangeSettings.MaxFileNameLength);

            profile.FolderName = _appContext.TenantRoot.CreateUniqueDirectoryName(ImportFileRoot, folderName);

            _db.ImportProfiles.Add(profile);

            // Get the import profile ID.
            await _db.SaveChangesAsync();

            task.Alias = profile.Id.ToString();

            // INFO: (mc) (core) Isolation --> always use ITaskStore to interact with task storage (which is DB in our case by default, but can be memory in future).
            await _taskStore.UpdateTaskAsync(task);

            // Finally update export profile.
            await _db.SaveChangesAsync();

            return profile;
        }

        public virtual async Task DeleteImportProfileAsync(ImportProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            await _db.LoadReferenceAsync(profile, x => x.Task);

            var directory = await GetImportDirectoryAsync(profile);

            _db.ImportProfiles.Remove(profile);

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

        public virtual async Task<int> DeleteUnusedImportDirectoriesAsync()
        {
            var numFolders = 0;
            var tenantRoot = _appContext.TenantRoot;

            var importProfileFolders = await _db.ImportProfiles
                .ApplyStandardFilter()
                .Select(x => x.FolderName)
                .ToListAsync();

            var dir = await tenantRoot.GetDirectoryAsync(ImportFileRoot);
            if (dir.Exists)
            {
                foreach (var subdir in dir.EnumerateDirectories())
                {
                    if (!importProfileFolders.Contains(subdir.Name))
                    {
                        dir.FileSystem.ClearDirectory(subdir, true, TimeSpan.Zero);
                        numFolders++;
                    }
                }
            }

            return numFolders;
        }

        #region Localized entity properties labels

        public virtual IDictionary<string, string> GetEntityPropertiesLabels(ImportEntityType entityType)
        {
            if (_entityProperties == null)
            {
                lock (_lock)
                {
                    if (_entityProperties == null)
                    {
                        _entityProperties = new Dictionary<ImportEntityType, Dictionary<string, string>>();

                        var allLanguages = _languageService.GetAllLanguages(true);
                        var allLanguageNames = allLanguages.ToDictionarySafe(x => x.LanguageCulture, x => x.GetLocalized(x => x.Name));

                        var localizableProperties = new Dictionary<ImportEntityType, string[]>
                        {
                            { ImportEntityType.Product, new[] { "Name", "ShortDescription", "FullDescription", "MetaKeywords", "MetaDescription", "MetaTitle", "SeName" } },
                            { ImportEntityType.Category, new[] { "Name", "FullName", "Description", "BottomDescription", "MetaKeywords", "MetaDescription", "MetaTitle", "SeName" } },
                            { ImportEntityType.Customer, Array.Empty<string>() },
                            { ImportEntityType.NewsletterSubscription, Array.Empty<string>() }
                        };

                        // There is no 'FindEntityTypeByDisplayName'.
                        var allEntityTypes = _db.Model.GetEntityTypes().ToDictionarySafe(x => x.DisplayName(), x => x, StringComparer.OrdinalIgnoreCase);

                        var addressProperties = allEntityTypes.Get(nameof(Address))?.GetProperties()
                            .Where(x => x.Name != "Id")
                            .Select(x => x.Name)
                            .ToList();

                        foreach (ImportEntityType importType in Enum.GetValues(typeof(ImportEntityType)))
                        {
                            if (!allEntityTypes.TryGetValue(importType.ToString(), out var efType) || efType == null)
                            {
                                throw new InvalidOperationException($"There is no entity set for ImportEntityType {importType}. Note, the enum value must equal the entity name.");
                            }

                            var names = efType.GetProperties()
                                .Where(x => x.Name != "Id")
                                .Select(x => x.Name)
                                .ToDictionarySafe(x => x, x => string.Empty, StringComparer.OrdinalIgnoreCase);

                            // Add property names missing for column mapping.
                            switch (importType)
                            {
                                case ImportEntityType.Product:
                                    names["SeName"] = string.Empty;
                                    names["Specification"] = "Specification";
                                    break;
                                case ImportEntityType.Category:
                                    names["SeName"] = string.Empty;
                                    names["TreePath"] = "Category tree path";
                                    break;
                                case ImportEntityType.Customer:
                                    foreach (var property in addressProperties)
                                    {
                                        names["BillingAddress." + property] = string.Empty;
                                        names["ShippingAddress." + property] = string.Empty;
                                    }
                                    break;
                            }

                            // Add localized property names.
                            var keys = names.Where(x => x.Value.IsEmpty()).Select(x => x.Key).ToArray();
                            foreach (var key in keys)
                            {
                                var localizedValue = GetLocalizedPropertyLabel(importType, key).NaIfEmpty();
                                names[key] = localizedValue;

                                if (localizableProperties[importType].Contains(key))
                                {
                                    foreach (var language in allLanguages)
                                    {
                                        names[$"{key}[{language.LanguageCulture}]"] = $"{localizedValue} {allLanguageNames[language.LanguageCulture]}";
                                    }
                                }
                            }

                            _entityProperties[importType] = names;
                        }
                    }
                }
            }

            return _entityProperties.ContainsKey(entityType) ? _entityProperties[entityType] : null;
        }

        private string GetLocalizedPropertyLabel(ImportEntityType entityType, string property)
        {
            if (property.IsEmpty() || _ignoreResourceKeys.Contains(property))
            {
                return property.SplitPascalCase();
            }

            string key = null;
            string prefixKey = null;

            if (property.StartsWith("BillingAddress."))
            {
                prefixKey = "Admin.Orders.Fields.BillingAddress";
            }
            else if (property.StartsWith("ShippingAddress."))
            {
                prefixKey = "Admin.Orders.Fields.ShippingAddress";
            }

            switch (entityType)
            {
                case ImportEntityType.Product:
                    key = "Admin.Catalog.Products.Fields." + property;
                    break;
                case ImportEntityType.Category:
                    key = "Admin.Catalog.Categories.Fields." + property;
                    break;
                case ImportEntityType.Customer:
                    key = (property.StartsWith("BillingAddress.") || property.StartsWith("ShippingAddress."))
                        ? "Address.Fields." + property.Substring(property.IndexOf('.') + 1)
                        : "Admin.Customers.Customers.Fields." + property;
                    break;
                case ImportEntityType.NewsletterSubscription:
                    key = "Admin.Promotions.NewsletterSubscriptions.Fields." + property;
                    break;
            }

            if (key.IsEmpty())
            {
                return string.Empty;
            }

            var result = GetResource(key);

            if (result.IsEmpty() && _otherResourceKeys.TryGetValue(property, out var otherKey))
            {
                result = GetResource(otherKey);
            }

            if (result.IsEmpty())
            {
                if (key.EndsWith("Id"))
                {
                    result = GetResource(key.Substring(0, key.Length - 2));
                }
                else if (key.EndsWith("Utc"))
                {
                    result = GetResource(key.Substring(0, key.Length - 3));
                }
            }

            if (result.IsEmpty())
            {
                Debug.WriteLine($"Missing string resource mapping for {entityType} - {property}");
                result = property.SplitPascalCase();
            }

            if (prefixKey.HasValue())
            {
                result = GetResource(prefixKey) + " - " + result;
            }

            return result;

            string GetResource(string resourceKey)
                => _localizationService.GetResource(resourceKey, 0, false, string.Empty, true);
        }

        /// <summary>
        /// Names of properties for which no localization exists.
        /// </summary>
        private static readonly HashSet<string> _ignoreResourceKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "IsSystemProduct", "SystemName", "LastForumVisit", "LastUserAgent", "LastUserDeviceType"
        };

        /// <summary>
        /// Mapping of property name to string ressource key.
        /// </summary>
        private static readonly Dictionary<string, string> _otherResourceKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Id", "Admin.Common.Entity.Fields.Id" },
            { "DisplayOrder", "Common.DisplayOrder" },
            { "HomePageDisplayOrder", "Common.DisplayOrder" },
            { "Deleted", "Admin.Common.Deleted" },
            { "WorkingLanguageId", "Common.Language" },
            { "CreatedOnUtc", "Common.CreatedOn" },
            { "BillingAddress.CreatedOnUtc", "Common.CreatedOn" },
            { "ShippingAddress.CreatedOnUtc", "Common.CreatedOn" },
            { "UpdatedOnUtc", "Common.UpdatedOn" },
            { "HasDiscountsApplied", "Admin.Catalog.Products.Fields.HasDiscountsApplied" },
            { "DefaultViewMode", "Admin.Configuration.Settings.Catalog.DefaultViewMode" },
            { "StoreId", "Admin.Common.Store" },
            { "LimitedToStores", "Admin.Common.Store.LimitedTo" },
            { "SubjectToAcl", "Admin.Common.CustomerRole.LimitedTo" },
            { "ParentGroupedProductId", "Admin.Catalog.Products.Fields.AssociatedToProductName" },
            { "PasswordFormatId", "Admin.Configuration.Settings.CustomerUser.DefaultPasswordFormat" },
            { "LastIpAddress", "Admin.Customers.Customers.Fields.IPAddress" },
            { "CustomerNumber", "Account.Fields.CustomerNumber" },
            { "BirthDate", "Admin.Customers.Customers.Fields.DateOfBirth" },
            { "Salutation", "Address.Fields.Salutation" },
            { "VisibleIndividually", "Admin.Catalog.Products.Fields.Visibility" },
            { "IsShippingEnabled", "Admin.Catalog.Products.Fields.IsShipEnabled" },
            { "MetaDescription", "Admin.Configuration.Seo.MetaDescription" },
            { "MetaKeywords", "Admin.Configuration.Seo.MetaKeywords" },
            { "MetaTitle", "Admin.Configuration.Seo.MetaTitle" },
            { "SeName", "Admin.Configuration.Seo.SeName" },
            { "TaxDisplayTypeId", "Admin.Customers.CustomerRoles.Fields.TaxDisplayType" },
            { "MainPictureId", "FileUploader.MultiFiles.MainMediaFile" },
            { "MediaFileId", "Common.Image" },
            { "BillingAddressId", "Admin.Orders.Fields.BillingAddress" },
            { "ShippingAddressId", "Admin.Orders.Fields.ShippingAddress" }
        };

        #endregion
    }
}
