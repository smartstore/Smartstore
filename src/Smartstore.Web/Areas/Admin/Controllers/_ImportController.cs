using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Import;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Security;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Utilities;
using Smartstore.Web.Controllers;

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

        #endregion
    }
}
