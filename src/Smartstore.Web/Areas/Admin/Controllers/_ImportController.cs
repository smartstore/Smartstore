using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            var profile = await _db.ImportProfiles.FindByIdAsync(id, false);
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
            var profile = await _db.ImportProfiles.FindByIdAsync(model.Id, true);
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
        public JsonResult FileUpload(int id)
        {
            // TODO: (mg) (core) complete ImportController (FileUpload missing).

            throw new NotImplementedException();
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

            // TODO: (mg) (core) complete PrepareProfileModel.
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
