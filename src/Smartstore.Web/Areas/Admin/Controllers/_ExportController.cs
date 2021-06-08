using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Export;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Scheduling;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class ExportController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IExportProfileService _exportProfileService;
        private readonly IProviderManager _providerManager;

        public ExportController(
            SmartDbContext db,
            IExportProfileService exportProfileService,
            IProviderManager providerManager)
        {
            _db = db;
            _exportProfileService = exportProfileService;
            _providerManager = providerManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> List()
        {
            var model = new List<ExportProfileModel>();

            var providers = _providerManager.GetAllProviders<IExportProvider>()
                .Where(x => x.Value != null && !x.Metadata.IsHidden)
                .OrderBy(x => x.Metadata.FriendlyName)
                .ToDictionarySafe(x => x.Metadata.SystemName);

            var profiles = await _db.ExportProfiles
                .AsNoTracking()
                .Include(x => x.Task)
                .OrderBy(x => x.IsSystemProfile)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var lastExecutionInfos = (await _db.TaskExecutionInfos
                .AsNoTracking()
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

                    await PrepareProfileModel(profileModel, profile, provider, lastExecutionInfo);

                    model.Add(profileModel);
                }
            }

            return View(model);
        }

        #region Utilities

        private async Task PrepareProfileModel(
            ExportProfileModel model,
            ExportProfile profile,
            Provider<IExportProvider> provider,
            TaskExecutionInfo lastExecutionInfo)
        {
            MiniMapper.Map(profile, model);

            var logFile = await _exportProfileService.GetLogFileAsync(profile);
            var descriptor = provider.Metadata.ModuleDescriptor;

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
                // TODO: (mg) (core) missing ModuleDescriptor properties.
                //ThumbnailUrl = GetThumbnailUrl(provider)
                //FriendlyName = _pluginMediator.GetLocalizedFriendlyName(provider.Metadata),
                //Description = _pluginMediator.GetLocalizedDescription(provider.Metadata),
                //Url = descriptor?.Url,
                //Author = descriptor?.Author,
                //Version = descriptor?.Version?.ToString()
            };
        }


        #endregion
    }
}
