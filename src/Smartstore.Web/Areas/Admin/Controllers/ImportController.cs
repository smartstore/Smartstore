using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Import;
using Smartstore.Admin.Models.Scheduling;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Csv;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Identity;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Utilities;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Admin.Controllers
{
    public class ImportController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IImportProfileService _importProfileService;
        private readonly ITaskStore _taskStore;
        private readonly ITaskScheduler _taskScheduler;

        public ImportController(
            SmartDbContext db,
            IImportProfileService importProfileService,
            ITaskStore taskStore,
            ITaskScheduler taskScheduler)
        {
            _db = db;
            _importProfileService = importProfileService;
            _taskStore = taskStore;
            _taskScheduler = taskScheduler;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Import.Read)]
        public async Task<IActionResult> List()
        {
            var model = new ImportProfileListModel();

            var lastExecutionInfos = (await _taskStore.GetExecutionInfoQuery(false)
                .ApplyCurrentMachineNameFilter()
                .ApplyTaskFilter(0, true)
                .ToListAsync())
                .ToDictionarySafe(x => x.TaskDescriptorId);

            var profiles = await _db.ImportProfiles
                .AsNoTracking()
                .ApplyStandardFilter()
                .ToListAsync();

            foreach (var profile in profiles)
            {
                var profileModel = new ImportProfileModel();

                lastExecutionInfos.TryGetValue(profile.TaskId, out var lastExecutionInfo);
                await PrepareProfileModel(profileModel, profile, lastExecutionInfo, false);

                profileModel.TaskModel = await profile.Task.MapAsync(lastExecutionInfo);

                model.Profiles.Add(profileModel);
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.Import.Read)]
        public async Task<IActionResult> ProfileImportResult(int profileId)
        {
            var profile = await _db.ImportProfiles.FindByIdAsync(profileId, false);
            if (profile != null)
            {
                var model = XmlHelper.Deserialize<SerializableImportResult>(profile.ResultInfo);

                return PartialView(model);
            }

            return new EmptyResult();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Import.Create)]
        public async Task<IActionResult> Create(ImportProfileModel model)
        {
            try
            {
                if (model.TempFileName.HasValue() && !PathUtility.HasInvalidFileNameChars(model.TempFileName))
                {
                    var root = Services.ApplicationContext.TenantRoot;
                    var tempDir = Services.ApplicationContext.GetTenantTempDirectory();
                    var source = await tempDir.GetFileAsync(model.TempFileName);

                    if (source.Exists)
                    {
                        var profile = await _importProfileService.InsertImportProfileAsync(model.TempFileName, model.Name, model.EntityType);
                        if (profile?.Id > 0)
                        {
                            var dir = await _importProfileService.GetImportDirectoryAsync(profile, "Content", true);
                            var target = await dir.GetFileAsync(source.Name);

                            using (var sourceStream = await source.OpenReadAsync())
                            using (var targetStream = await target.OpenWriteAsync())
                            {
                                await sourceStream.CopyToAsync(targetStream);
                            }

                            await tempDir.FileSystem.TryDeleteFileAsync(source);

                            return RedirectToAction(nameof(Edit), new { id = profile.Id });
                        }
                    }
                    else
                    {
                        NotifyError(T("Admin.DataExchange.Import.MissingImportFile"));
                    }
                }
                else
                {
                    NotifyError(T("Admin.Common.InvalidFileName"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Import.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var profile = await _db.ImportProfiles
                .Include(x => x.Task)
                .FindByIdAsync(id, false);

            if (profile == null)
            {
                return NotFound();
            }

            var model = new ImportProfileModel();
            var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(profile.TaskId, true);
            await PrepareProfileModel(model, profile, lastExecutionInfo, true);

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("save", "save-continue"), ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Import.Update)]
        public async Task<IActionResult> Edit(ImportProfileModel model, bool continueEditing, IFormCollection form)
        {
            var profile = await _db.ImportProfiles
                .Include(x => x.Task)
                .FindByIdAsync(model.Id);

            if (profile == null)
            {
                return NotFound();
            }

            var map = new ColumnMap();
            var hasErrors = false;
            var resetMappings = false;

            try
            {
                var propertyKeyPrefix = "ColumnMapping.Property.";
                var allPropertyKeys = form.Keys.Where(x => x.HasValue() && x.StartsWith(propertyKeyPrefix));

                if (allPropertyKeys.Any())
                {
                    var entityProperties = _importProfileService.GetEntityPropertiesLabels(profile.EntityType);

                    foreach (var key in allPropertyKeys)
                    {
                        var index = key[propertyKeyPrefix.Length..];
                        var property = form[key].ToString();
                        var column = form["ColumnMapping.Column." + index].ToString();
                        var defaultValue = form["ColumnMapping.Default." + index].ToString();

                        if (column.IsEmpty())
                        {
                            // Tell mapper to explicitly ignore the property.
                            map.AddMapping(property, null, property, "[IGNOREPROPERTY]");
                        }
                        else if (!column.EqualsNoCase(property) || defaultValue.HasValue())
                        {
                            if (defaultValue.HasValue() && GetDisabledDefaultFieldNames(profile).Contains(property))
                            {
                                defaultValue = null;
                            }

                            map.AddMapping(property, null, column, defaultValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                hasErrors = true;
                NotifyError(ex, true, false);
            }

            if (!ModelState.IsValid || hasErrors)
            {
                var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(profile.TaskId, true);
                await PrepareProfileModel(model, profile, lastExecutionInfo, true, map);
                return View(model);
            }

            profile.Name = model.Name;
            profile.EntityType = model.EntityType;
            profile.Enabled = model.Enabled;
            profile.ImportRelatedData = model.ImportRelatedData;
            profile.Skip = model.Skip ?? 0;
            profile.Take = model.Take ?? 0;
            profile.UpdateOnly = model.UpdateOnly;
            profile.KeyFieldNames = model.KeyFieldNames == null ? null : string.Join(',', model.KeyFieldNames);

            try
            {
                if (profile.FileType == ImportFileType.Csv && model.CsvConfiguration != null)
                {
                    var csvConverter = new CsvConfigurationConverter();
                    var oldCsvConfig = csvConverter.ConvertFrom<CsvConfiguration>(profile.FileTypeConfiguration);
                    var oldDelimiter = oldCsvConfig?.Delimiter.ToString();

                    // Auto reset mappings cause they are invalid.
                    // INFO: delimiter can be whitespaced, so no oldDelimter.HasValue() etc.
                    resetMappings = oldDelimiter != model.CsvConfiguration.Delimiter && profile.ColumnMapping.HasValue();

                    profile.FileTypeConfiguration = csvConverter.ConvertTo(model.CsvConfiguration.Clone());
                }
                else
                {
                    profile.FileTypeConfiguration = null;
                }

                if (resetMappings)
                {
                    profile.ColumnMapping = null;
                }
                else
                {
                    var mapConverter = new ColumnMapConverter();
                    profile.ColumnMapping = mapConverter.ConvertTo(map);
                }

                if (model.ExtraData != null)
                {
                    var extraData = model.ExtraData.IsDefault()
                        ? null
                        : await MapperFactory.MapAsync<ImportProfileModel.ExtraDataModel, ImportExtraData>(model.ExtraData);

                    profile.ExtraData = XmlHelper.Serialize(extraData);
                }
            }
            catch (Exception ex)
            {
                hasErrors = true;
                NotifyError(ex, true, false);
            }

            if (!hasErrors)
            {
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

                if (resetMappings)
                {
                    NotifyWarning(T("Admin.DataExchange.ColumnMapping.Validate.MappingsReset"));
                }
            }

            return continueEditing
                ? RedirectToAction(nameof(Edit), new { id = profile.Id })
                : RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Import.Update)]
        public async Task<IActionResult> ResetColumnMappings(int id)
        {
            var profile = await _db.ImportProfiles.FindByIdAsync(id, true);
            if (profile == null)
            {
                return NotFound();
            }

            profile.ColumnMapping = null;
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));

            return RedirectToAction(nameof(Edit), new { id = profile.Id });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Import.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var profile = await _db.ImportProfiles.FindByIdAsync(id, true);
            if (profile == null)
            {
                return NotFound();
            }

            try
            {
                await _importProfileService.DeleteImportProfileAsync(profile);

                NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));

                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id = profile.Id });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Import.Delete)]
        public async Task<IActionResult> DeleteImportFile(int id, string name)
        {
            var profile = await _db.ImportProfiles.FindByIdAsync(id, false);
            if (profile == null)
            {
                return NotFound();
            }

            var dir = await _importProfileService.GetImportDirectoryAsync(profile, "Content");
            var file = await dir.GetFileAsync(name);

            await dir.FileSystem.TryDeleteFileAsync(file);

            return RedirectToAction(nameof(Edit), new { id });
        }

        [Permission(Permissions.Configuration.Import.Read)]
        public async Task<IActionResult> DownloadLogFile(int id)
        {
            var profile = await _db.ImportProfiles.FindByIdAsync(id, false);
            if (profile == null)
            {
                return NotFound();
            }

            var dir = await _importProfileService.GetImportDirectoryAsync(profile);
            var logFile = await dir.GetFileAsync("log.txt");
            if (logFile.Exists)
            {
                try
                {
                    var stream = await logFile.OpenReadAsync();
                    return new FileStreamResult(stream, MediaTypeNames.Text.Plain);
                }
                catch (IOException)
                {
                    NotifyWarning(T("Admin.Common.FileInUse"));
                }
            }

            return RedirectToAction(nameof(List));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadImportFile(int id, string name)
        {
            string message = null;

            if (name.HasValue() && !PathUtility.HasInvalidFileNameChars(name))
            {
                if (await Services.Permissions.AuthorizeAsync(Permissions.Configuration.Import.Read))
                {
                    var profile = await _db.ImportProfiles.FindByIdAsync(id, false);
                    if (profile != null)
                    {
                        var dir = await _importProfileService.GetImportDirectoryAsync(profile, "Content");
                        var file = await dir.GetFileAsync(name);

                        if (!file.Exists)
                        {
                            file = await dir.Parent.GetFileAsync(name);
                        }

                        if (file.Exists)
                        {
                            try
                            {
                                var stream = await file.OpenReadAsync();
                                return new FileStreamResult(stream, MimeTypes.MapNameToMimeType(file.PhysicalPath))
                                {
                                    FileDownloadName = file.Name
                                };
                            }
                            catch (IOException)
                            {
                                message = T("Admin.Common.FileInUse");
                            }
                        }
                    }
                }
                else
                {
                    message = T("Admin.AccessDenied.Description");
                }
            }
            else
            {
                message = "Invalid file name.";
            }

            if (message.IsEmpty())
            {
                message = T("Admin.Common.ResourceNotFound");
            }

            return File(message.GetBytes(), MediaTypeNames.Text.Plain, "DownloadImportFile.txt");
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Import.Update)]
        public async Task<IActionResult> FileUpload(int id)
        {
            if (!Request.Form.Files.Any())
            {
                throw new BadHttpRequestException(T("Common.NoFileUploaded"));
            }

            var success = false;
            string error = null;
            var source = Request.Form.Files[0];

            try
            {
                if (id == 0)
                {
                    var tempDir = Services.ApplicationContext.GetTenantTempDirectory();
                    _ = await CopyFile(tempDir, source);

                    success = true;
                }
                else
                {
                    var profile = await _db.ImportProfiles.FindByIdAsync(id, true);
                    var file = (await _importProfileService.GetImportFilesAsync(profile, false)).FirstOrDefault();

                    if (file != null && !Path.GetExtension(source.FileName).EqualsNoCase(file.File.Extension))
                    {
                        throw new InvalidOperationException(T("Admin.Common.FileTypeMustEqual", file.File.Extension[1..].ToUpper()));
                    }

                    var dir = await _importProfileService.GetImportDirectoryAsync(profile, "Content", true);
                    var target = await CopyFile(dir, source);

                    success = true;

                    var fileType = Path.GetExtension(source.FileName).EqualsNoCase(".xlsx") ? ImportFileType.Xlsx : ImportFileType.Csv;
                    if (fileType != profile.FileType)
                    {
                        var tmp = new ImportFile(target);
                        if (!tmp.RelatedType.HasValue)
                        {
                            profile.FileType = fileType;
                            await _db.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                error = ex.Message;
            }

            if (!success && error.IsEmpty())
            {
                error = T("Admin.Common.UploadFileFailed");
            }

            return Json(new
            {
                success,
                error,
                name = source.FileName,
                ext = Path.GetExtension(source.FileName)
            });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Import.Execute)]
        public async Task<IActionResult> Execute(int id)
        {
            var profile = await _db.ImportProfiles.FindByIdAsync(id, false);
            if (profile == null)
            {
                return NotFound();
            }

            var taskParams = new Dictionary<string, string>
            {
                { TaskExecutor.CurrentCustomerIdParamName, Services.WorkContext.CurrentCustomer.Id.ToString() },
                { TaskExecutor.CurrentStoreIdParamName, Services.StoreContext.CurrentStore.Id.ToString() }
            };

            _ = _taskScheduler.RunSingleTaskAsync(profile.TaskId, taskParams);

            NotifyInfo(T("Admin.System.ScheduleTasks.RunNow.Progress.DataImportTask"));

            return RedirectToReferrer(null, () => RedirectToAction(nameof(List)));
        }

        #region Data exchange settings

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public IActionResult DataExchangeSettings(DataExchangeSettings settings)
        {
            var model = new DataExchangeSettingsModel();
            MiniMapper.Map(settings, model);

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public IActionResult DataExchangeSettings(DataExchangeSettings settings, DataExchangeSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return DataExchangeSettings(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            NotifySuccess(T("Admin.Configuration.Updated"));
            return RedirectToAction(nameof(DataExchangeSettings));
        }

        #endregion

        #region Utilities

        private async Task PrepareProfileModel(
            ImportProfileModel model,
            ImportProfile profile,
            TaskExecutionInfo lastExecutionInfo,
            bool prepareForEdit,
            ColumnMap invalidMap = null)
        {
            var dir = await _importProfileService.GetImportDirectoryAsync(profile);
            var logFile = await dir.GetFileAsync("log.txt");

            model.Id = profile.Id;
            model.Name = profile.Name;
            model.EntityType = profile.EntityType;
            model.Enabled = profile.Enabled;
            model.ImportRelatedData = profile.ImportRelatedData;
            model.Skip = profile.Skip == 0 ? null : profile.Skip;
            model.Take = profile.Take == 0 ? null : profile.Take;
            model.UpdateOnly = profile.UpdateOnly;
            model.KeyFieldNames = profile.KeyFieldNames.SplitSafe(',').Distinct().ToArray();
            model.TaskId = profile.TaskId;
            model.TaskName = profile.Task.Name.NaIfEmpty();
            model.IsTaskRunning = lastExecutionInfo?.IsRunning ?? false;
            model.IsTaskEnabled = profile.Task.Enabled;
            model.LogFileExists = logFile.Exists;
            model.FolderName = PathUtility.MakeRelativePath(CommonHelper.ContentRoot.Root, dir.PhysicalPath);
            model.EntityTypeName = Services.Localization.GetLocalizedEnum(profile.EntityType);
            model.ExistingFiles = await _importProfileService.GetImportFilesAsync(profile);
            model.FileType = profile.FileType;

            foreach (var file in model.ExistingFiles.Where(x => x.RelatedType.HasValue))
            {
                file.Label = T("Admin.Common.Data") + " " + Services.Localization.GetLocalizedEnum(file.RelatedType.Value);
            }

            if (profile.ResultInfo.HasValue())
            {
                model.ImportResult = XmlHelper.Deserialize<SerializableImportResult>(profile.ResultInfo);
            }

            if (!prepareForEdit)
            {
                return;
            }

            var csvConfiguration = (profile.FileType == ImportFileType.Csv
                ? new CsvConfigurationConverter().ConvertFrom<CsvConfiguration>(profile.FileTypeConfiguration)
                : null) ?? CsvConfiguration.ExcelFriendlyConfiguration;

            model.CsvConfiguration = new CsvConfigurationModel(csvConfiguration)
            {
                Validate = profile.FileType == ImportFileType.Csv
            };

            // Common configuration.
            var extraData = XmlHelper.Deserialize<ImportExtraData>(profile.ExtraData);
            model.ExtraData = await MapperFactory.MapAsync<ImportExtraData, ImportProfileModel.ExtraDataModel>(extraData);

            // Column mapping.
            try
            {
                string[] disabledDefaultFieldNames = GetDisabledDefaultFieldNames(profile);
                var mapConverter = new ColumnMapConverter();
                var storedMap = mapConverter.ConvertFrom<ColumnMap>(profile.ColumnMapping);
                var map = (invalidMap ?? storedMap) ?? new ColumnMap();
                var sourceColumns = new List<ColumnMappingItemModel>();

                // Property name to localized property name.
                var allProperties = _importProfileService.GetEntityPropertiesLabels(profile.EntityType) ?? new Dictionary<string, string>();

                string[] availableKeyFieldNames = null;
                switch (profile.EntityType)
                {
                    case ImportEntityType.Product:
                        availableKeyFieldNames = ProductImporter.SupportedKeyFields;
                        break;
                    case ImportEntityType.Category:
                        availableKeyFieldNames = CategoryImporter.SupportedKeyFields;
                        break;
                    case ImportEntityType.Manufacturer:
                        availableKeyFieldNames = ManufacturerImporter.SupportedKeyFields;
                        break;
                    case ImportEntityType.Customer:
                        availableKeyFieldNames = CustomerImporter.SupportedKeyFields;
                        break;
                    case ImportEntityType.NewsletterSubscription:
                        availableKeyFieldNames = NewsletterSubscriptionImporter.SupportedKeyFields;
                        break;
                    default:
                        availableKeyFieldNames = [];
                        break;
                }

                ViewBag.KeyFieldNames = availableKeyFieldNames
                    .Select(x =>
                    {
                        var item = new SelectListItem { Value = x, Text = x };

                        if (x == "Id")
                            item.Text = T("Admin.Common.Entity.Fields.Id");
                        else if (allProperties.TryGetValue(x, out string label))
                            item.Text = label;

                        return item;
                    })
                    .ToList();

                ViewBag.EntityProperties = allProperties
                    .OrderBy(x => x.Value)
                    .Select(x => new ColumnMappingItemModel
                    {
                        Property = x.Key,
                        PropertyDescription = x.Value,
                        IsDefaultDisabled = IsDefaultValueDisabled(x.Key, disabledDefaultFieldNames)
                    })
                    .ToList();

                var columnMappings = map.Mappings
                    .Select(x =>
                    {
                        var mapping = new ColumnMappingItemModel
                        {
                            Column = x.Value.MappedName,
                            Property = x.Key,
                            Default = x.Value.Default
                        };

                        if (x.Value.IgnoreProperty)
                        {
                            // Explicitly ignore the property.
                            mapping.Column = null;
                            mapping.Default = null;
                        }

                        mapping.PropertyDescription = GetPropertyDescription(allProperties, mapping.Property);
                        mapping.IsDefaultDisabled = IsDefaultValueDisabled(mapping.Property, disabledDefaultFieldNames);

                        return mapping;
                    })
                    .ToList();

                var file = model.ExistingFiles.FirstOrDefault(x => !x.RelatedType.HasValue);
                if (file != null)
                {
                    using var stream = await file.File.OpenReadAsync();
                    var dataTable = LightweightDataTable.FromFile(file.File.Name, stream, stream.Length, csvConfiguration, 0, 1);

                    foreach (var column in dataTable.Columns.Where(x => x.Name.HasValue()))
                    {
                        ColumnMap.ParseSourceName(column.Name, out string columnWithoutIndex, out string columnIndex);

                        sourceColumns.Add(new ColumnMappingItemModel
                        {
                            Index = dataTable.Columns.IndexOf(column),
                            Column = column.Name,
                            ColumnWithoutIndex = columnWithoutIndex,
                            ColumnIndex = columnIndex,
                            PropertyDescription = GetPropertyDescription(allProperties, column.Name)
                        });

                        // Auto map where field equals property name.
                        if (!columnMappings.Any(x => x.Column == column.Name))
                        {
                            var kvp = allProperties.FirstOrDefault(x => x.Key.EqualsNoCase(column.Name));
                            if (kvp.Key.IsEmpty())
                            {
                                var alternativeName = LightweightDataTable.GetAlternativeColumnNameFor(column.Name);
                                kvp = allProperties.FirstOrDefault(x => x.Key.EqualsNoCase(alternativeName));
                            }

                            if (kvp.Key.HasValue() && !columnMappings.Any(x => x.Property == kvp.Key))
                            {
                                columnMappings.Add(new ColumnMappingItemModel
                                {
                                    Column = column.Name,
                                    Property = kvp.Key,
                                    PropertyDescription = kvp.Value,
                                    IsDefaultDisabled = IsDefaultValueDisabled(kvp.Key, disabledDefaultFieldNames)
                                });
                            }
                        }
                    }
                }

                ViewBag.SourceColumns = sourceColumns
                    .OrderBy(x => x.PropertyDescription)
                    .ToList();

                ViewBag.ColumnMappings = columnMappings
                    .OrderBy(x => x.PropertyDescription)
                    .ToList();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        private static async Task<IFile> CopyFile(IDirectory directory, IFormFile source)
        {
            await directory.FileSystem.TryDeleteFileAsync(await directory.GetFileAsync(source.FileName));

            var target = await directory.GetFileAsync(source.FileName);

            using (var sourceStream = source.OpenReadStream())
            using (var targetStream = await target.OpenWriteAsync())
            {
                await sourceStream.CopyToAsync(targetStream);
            }

            target = await directory.GetFileAsync(source.FileName);

            var (isValidFile, message) = await LightweightDataTable.IsValidFileAsync(target);
            if (!isValidFile)
            {
                throw new Exception(message);
            }

            return target;
        }

        private static bool IsDefaultValueDisabled(string property, string[] disabledFieldNames)
        {
            if (disabledFieldNames.Contains(property))
            {
                return true;
            }

            if (ColumnMap.ParseSourceName(property, out string columnWithoutIndex, out _))
            {
                return disabledFieldNames.Contains(columnWithoutIndex);
            }

            return false;
        }

        private static string GetPropertyDescription(IDictionary<string, string> allProperties, string property)
        {
            if (property.HasValue() && allProperties.TryGetValue(property, out var description) && description.HasValue())
            {
                return description;
            }

            return property;
        }

        private static string[] GetDisabledDefaultFieldNames(ImportProfile profile)
        {
            switch (profile.EntityType)
            {
                case ImportEntityType.Product:
                    return [nameof(Product.Name), nameof(Product.Sku), nameof(Product.ManufacturerPartNumber), nameof(Product.Gtin), "SeName"];
                case ImportEntityType.Category:
                    return [nameof(Category.Name), "SeName"];
                case ImportEntityType.Manufacturer:
                    return [nameof(Manufacturer.Name), "SeName"];
                case ImportEntityType.Customer:
                    return [nameof(Customer.CustomerGuid), nameof(Customer.Email)];
                case ImportEntityType.NewsletterSubscription:
                    return [nameof(NewsletterSubscription.Email)];
                default:
                    return [];
            }
        }

        #endregion
    }
}
