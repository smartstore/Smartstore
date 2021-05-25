using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Engine;
using Smartstore.IO;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class ImportProfileService : IImportProfileService
    {
        private const string _importFileRoot = "ImportProfiles";

        private readonly SmartDbContext _db;
        private readonly IApplicationContext _appContext;
        private readonly ILocalizationService _localizationService;
        private readonly DataExchangeSettings _dataExchangeSettings;

        public ImportProfileService(
            SmartDbContext db,
            IApplicationContext appContext,
            ILocalizationService localizationService,
            DataExchangeSettings dataExchangeSettings)
        {
            _db = db;
            _appContext = appContext;
            _localizationService = localizationService;
            _dataExchangeSettings = dataExchangeSettings;
        }

        public virtual async Task<IDirectory> GetImportDirectoryAsync(ImportProfile profile, string subpath = null, bool createIfNotExists = false)
        {
            Guard.NotNull(profile, nameof(profile));

            var root = _appContext.TenantRoot;
            var path = root.PathCombine(_importFileRoot, profile.FolderName, subpath.EmptyNull());

            if (createIfNotExists)
            {
                var _ = await root.TryCreateDirectoryAsync(path);
            }

            return await root.GetDirectoryAsync(path);
        }

        public virtual async Task<IList<ImportFile>> GetImportFilesAsync(ImportProfile profile, bool includeRelatedFiles = true)
        {
            var result = new List<ImportFile>();
            var directory = await GetImportDirectoryAsync(profile, "Content");

            if (directory.Exists)
            {
                var files = await directory.FileSystem.EnumerateFilesAsync(directory.SubPath).ToListAsync();
                if (files.Any())
                {
                    foreach (var file in files)
                    {
                        var importFile = new ImportFile(file);
                        if (!includeRelatedFiles && !importFile.RelatedType.HasValue)
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
            var defaultNamesStr = await _localizationService.GetResourceAsync("Admin.DataExchange.Import.DefaultProfileNames");
            var defaultNames = defaultNamesStr.SplitSafe(";").ToArray();
            var profileCount = 1 + await _db.ImportProfiles.CountAsync(x => x.EntityTypeId == (int)entityType);

            var result = defaultNames.ElementAtOrDefault((int)entityType).NullEmpty() ?? entityType.ToString();
            return result + " " + profileCount.ToString();
        }

        public virtual async Task<ImportProfile> InsertImportProfileAsync(string fileName, string name, ImportEntityType entityType)
        {
            Guard.NotEmpty(fileName, nameof(fileName));

            if (name.IsEmpty())
            {
                name = await GetNewProfileNameAsync(entityType);
            }

            var task = new TaskDescriptor
            {
                CronExpression = "0 */24 * * *",
                Type = typeof(DataImportTask).AssemblyQualifiedNameWithoutVersion(),
                Enabled = false,
                StopOnError = false,
                IsHidden = true,
                Name = name + " Task"
            };

            _db.TaskDescriptors.Add(task);

            // Get the task ID.
            await _db.SaveChangesAsync();

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

            // TODO: (mg) (core) apply default key fields for new import profile.
            //switch (entityType)
            //{
            //    case ImportEntityType.Product:
            //        profile.KeyFieldNames = string.Join(",", ProductImporter.DefaultKeyFields);
            //        break;
            //    case ImportEntityType.Category:
            //        profile.KeyFieldNames = string.Join(",", CategoryImporter.DefaultKeyFields);
            //        break;
            //    case ImportEntityType.Customer:
            //        profile.KeyFieldNames = string.Join(",", CustomerImporter.DefaultKeyFields);
            //        break;
            //    case ImportEntityType.NewsLetterSubscription:
            //        profile.KeyFieldNames = string.Join(",", NewsLetterSubscriptionImporter.DefaultKeyFields);
            //        break;
            //}

            var folderName = SeoHelper.BuildSlug(name, true, false, false)
                .ToValidPath()
                .Truncate(_dataExchangeSettings.MaxFileNameLength);

            profile.FolderName = _appContext.TenantRoot.CreateUniqueDirectoryName(_importFileRoot, folderName);

            _db.ImportProfiles.Add(profile);

            // Get the import profile ID.
            await _db.SaveChangesAsync();

            task.Alias = profile.Id.ToString();

            // Finally update task and export profile.
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

            if (profile.Task != null)
            {
                _db.TaskDescriptors.Remove(profile.Task);
            }

            _db.ImportProfiles.Remove(profile);

            await _db.SaveChangesAsync();

            if (directory.Exists)
            {
                directory.FileSystem.ClearDirectory(directory, true, TimeSpan.Zero);
            }
        }
    }
}
