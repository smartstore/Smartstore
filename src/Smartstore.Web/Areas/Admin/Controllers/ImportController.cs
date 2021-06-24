using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Import;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Csv;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Security;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Utilities;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Controllers
{
    public class ImportController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IImportProfileService _importProfileService;
        private readonly ITaskStore _taskStore;

        public ImportController(
            SmartDbContext db,
            IImportProfileService importProfileService,
            ITaskStore taskStore)
        {
            _db = db;
            _importProfileService = importProfileService;
            _taskStore = taskStore;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
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
                .Include(x => x.Task)
                .AsNoTracking()
                .OrderBy(x => x.EntityTypeId)
                .ThenBy(x => x.Name)
                .ToListAsync();

            foreach (var profile in profiles)
            {
                var profileModel = new ImportProfileModel();

                lastExecutionInfos.TryGetValue(profile.TaskId, out var lastExecutionInfo);
                await PrepareProfileModel(profileModel, profile, lastExecutionInfo, false);

                // TODO: (mg) (core) create task model for import profile list.
                //profileModel.TaskModel = _adminModelHelper.CreateScheduleTaskModel(profile.Task, lastExecutionInfo) ?? new TaskModel();

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
            if (PathHelper.HasInvalidFileNameChars(model.TempFileName))
            {
                return BadRequest("Invalid file name.");
            }

            try
            {
                var root = Services.ApplicationContext.TenantRoot;
                var tenantTempDir = Services.ApplicationContext.GetTenantTempDirectory();
                var importFile = await tenantTempDir.GetFileAsync(model.TempFileName.EmptyNull());

                if (importFile.Exists)
                {
                    var profile = await _importProfileService.InsertImportProfileAsync(model.TempFileName, model.Name, model.EntityType);
                    if (profile?.Id > 0)
                    {
                        var dir = await _importProfileService.GetImportDirectoryAsync(profile, "Content", true);

                        await root.CopyFileAsync(importFile.SubPath, root.PathCombine(dir.SubPath, importFile.Name), true);
                        await root.TryDeleteFileAsync(importFile.SubPath);

                        return RedirectToAction("Edit", new { id = profile.Id });
                    }
                }
                else
                {
                    NotifyError(T("Admin.DataExchange.Import.MissingImportFile"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("List");
        }

        [Permission(Permissions.Configuration.Import.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            // INFO: (mg) (core) IIncludableQuery<T> has also extension method FindByIdAsync()
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
        public async Task<IActionResult> Edit(ImportProfileModel model, bool continueEditing, FormCollection form)
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
            profile.KeyFieldNames = model.KeyFieldNames == null ? null : string.Join(",", model.KeyFieldNames);

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
                    profile.ExtraData = XmlHelper.Serialize(new ImportExtraData
                    {
                        NumberOfPictures = model.ExtraData.NumberOfPictures
                    });
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
                ? RedirectToAction("Edit", new { id = profile.Id })
                : RedirectToAction("List");
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

            return RedirectToAction("Edit", new { id = profile.Id });
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

                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id = profile.Id });
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
            await dir.FileSystem.TryDeleteFileAsync(name);

            return RedirectToAction("Edit", new { id });
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

            return RedirectToAction("List");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadImportFile(int id, string name)
        {
            if (PathHelper.HasInvalidFileNameChars(name))
            {
                return BadRequest("Invalid file name.");
            }

            string message = null;

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

            if (message.IsEmpty())
            {
                message = T("Admin.Common.ResourceNotFound");
            }

            return File(Encoding.UTF8.GetBytes(message), MediaTypeNames.Text.Plain, "DownloadImportFile.txt");
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
            var tempFile = string.Empty;
            var sourceFile = Request.Form.Files[0];
            var fileName = sourceFile.FileName;

            if (id == 0)
            {
                var root = Services.ApplicationContext.TenantRoot;
                var tenantTempDir = Services.ApplicationContext.GetTenantTempDirectory();

                await tenantTempDir.FileSystem.TryDeleteFileAsync(fileName);
                var targetFile = await tenantTempDir.GetFileAsync(fileName);

                using (var sourceStream = sourceFile.OpenReadStream())
                using (var targetStream = targetFile.OpenWrite())
                {
                    await sourceStream.CopyToAsync(targetStream);
                }

                var (isValidFile, message) = await LightweightDataTable.IsValidFileAsync(targetFile);
                if (isValidFile)
                {
                    success = true;
                    tempFile = fileName;
                }
                else
                {
                    error = message;
                    await tenantTempDir.FileSystem.TryDeleteFileAsync(fileName);
                }
            }
            else
            {
                var profile = await _db.ImportProfiles.FindByIdAsync(id, true);
                if (profile != null)
                {
                    var files = await _importProfileService.GetImportFilesAsync(profile, false);
                    var file = files.FirstOrDefault();

                    if (file != null && !Path.GetExtension(sourceFile.FileName).EqualsNoCase(file.File.Extension))
                    {
                        error = T("Admin.Common.FileTypeMustEqual", file.File.Extension[1..].ToUpper());
                    }

                    if (error.IsEmpty())
                    {
                        var dir = await _importProfileService.GetImportDirectoryAsync(profile, "Content", true);
                        var targetFile = await dir.GetFileAsync(fileName);

                        using (var sourceStream = sourceFile.OpenReadStream())
                        using (var targetStream = targetFile.OpenWrite())
                        {
                            await sourceStream.CopyToAsync(targetStream);
                        }

                        var (isValidFile, message) = await LightweightDataTable.IsValidFileAsync(targetFile);
                        if (isValidFile)
                        {
                            success = true;

                            var fileType = Path.GetExtension(fileName).EqualsNoCase(".xlsx") ? ImportFileType.Xlsx : ImportFileType.Csv;
                            if (fileType != profile.FileType)
                            {
                                var tmp = new ImportFile(targetFile);
                                if (!tmp.RelatedType.HasValue)
                                {
                                    profile.FileType = fileType;
                                    await _db.SaveChangesAsync();
                                }
                            }
                        }
                        else
                        {
                            error = message;
                        }
                    }
                }
            }

            if (!success && error.IsEmpty())
            {
                error = T("Admin.Common.UploadFileFailed");
            }
            if (error.HasValue())
            {
                NotifyError(error);
            }

            return Json(new { success, tempFile, error, name = sourceFile.FileName, ext = Path.GetExtension(sourceFile.FileName) });
        }

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
            model.KeyFieldNames = profile.KeyFieldNames.SplitSafe(",").Distinct().ToArray();
            model.TaskId = profile.TaskId;
            model.TaskName = profile.Task.Name.NaIfEmpty();
            model.IsTaskRunning = lastExecutionInfo?.IsRunning ?? false;
            model.IsTaskEnabled = profile.Task.Enabled;
            model.LogFileExists = logFile.Exists;
            model.FolderName = dir.Name;
            model.EntityTypeName = await Services.Localization.GetLocalizedEnumAsync(profile.EntityType);
            model.ExistingFiles = await _importProfileService.GetImportFilesAsync(profile);

            foreach (var file in model.ExistingFiles.Where(x => x.RelatedType.HasValue))
            {
                file.Label = T("Admin.Common.Data") + " " + await Services.Localization.GetLocalizedEnumAsync(file.RelatedType.Value);
            }

            if (profile.ResultInfo.HasValue())
            {
                model.ImportResult = XmlHelper.Deserialize<SerializableImportResult>(profile.ResultInfo);
            }

            if (!prepareForEdit)
            {
                return;
            }

            CsvConfiguration csvConfiguration = null;

            if (profile.FileType == ImportFileType.Csv)
            {
                var csvConverter = new CsvConfigurationConverter();
                csvConfiguration = csvConverter.ConvertFrom<CsvConfiguration>(profile.FileTypeConfiguration) ?? CsvConfiguration.ExcelFriendlyConfiguration;

                model.CsvConfiguration = new CsvConfigurationModel(csvConfiguration);
            }
            else
            {
                csvConfiguration = CsvConfiguration.ExcelFriendlyConfiguration;
            }

            // Common configuration.
            var extraData = XmlHelper.Deserialize<ImportExtraData>(profile.ExtraData);
            model.ExtraData.NumberOfPictures = extraData.NumberOfPictures;

            // Column mapping.
            try
            {
                string[] availableKeyFieldNames = null;
                string[] disabledDefaultFieldNames = GetDisabledDefaultFieldNames(profile);
                var mapConverter = new ColumnMapConverter();
                var storedMap = mapConverter.ConvertFrom<ColumnMap>(profile.ColumnMapping);
                var map = (invalidMap ?? storedMap) ?? new ColumnMap();
                var sourceColumns = new List<ColumnMappingItemModel>();

                // Property name to localized property name.
                var allProperties = _importProfileService.GetEntityPropertiesLabels(profile.EntityType) ?? new Dictionary<string, string>();

                switch (profile.EntityType)
                {
                    case ImportEntityType.Product:
                        availableKeyFieldNames = ProductImporter.SupportedKeyFields;
                        break;
                    case ImportEntityType.Category:
                        availableKeyFieldNames = CategoryImporter.SupportedKeyFields;
                        break;
                    case ImportEntityType.Customer:
                        availableKeyFieldNames = CustomerImporter.SupportedKeyFields;
                        break;
                    case ImportEntityType.NewsLetterSubscription:
                        availableKeyFieldNames = NewsletterSubscriptionImporter.SupportedKeyFields;
                        break;
                }

                ViewBag.KeyFieldNames = availableKeyFieldNames
                    .Select(x =>
                    {
                        var item = new SelectListItem { Value = x, Text = x };

                        if (x == "Id")
                            item.Text = T("Admin.Common.Entity.Fields.Id");
                        else if (allProperties.ContainsKey(x))
                            item.Text = allProperties[x];

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

                    ViewBag.SourceColumns = sourceColumns
                        .OrderBy(x => x.PropertyDescription)
                        .ToList();

                    ViewBag.ColumnMappings = columnMappings
                        .OrderBy(x => x.PropertyDescription)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex, true, false);
            }
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
            return profile.EntityType switch
            {
                ImportEntityType.Product => new[] { "Name", "Sku", "ManufacturerPartNumber", "Gtin", "SeName" },
                ImportEntityType.Category => new[] { "Name", "SeName" },
                ImportEntityType.Customer => new[] { "CustomerGuid", "Email" },
                ImportEntityType.NewsLetterSubscription => new[] { "Email" },
                _ => Array.Empty<string>(),
            };
        }

        #endregion
    }
}
