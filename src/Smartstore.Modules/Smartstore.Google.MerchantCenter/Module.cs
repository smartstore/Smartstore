global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentValidation;
global using Smartstore.Core.Localization;
global using Smartstore.Web.Modelling;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Engine.Modularity;
using Smartstore.Google.MerchantCenter.Providers;
using Smartstore.Http;

// TODO: (mh) (core) Please check whether Google provides more up-to-date taxonomy files.
// TODO: (mh) (core) Language resource keys for module displayname and description follow conventions:
//       "Plugins.FriendlyName.SmartStore.GoogleMerchantCenter" does not meet this convention:
//       It needs to be: "Plugins.FriendlyName.Smartstore.Google.MerchantCenter"
//       Please verify, fix here and in all other renamed modules.

namespace Smartstore.Google.MerchantCenter
{
    internal class Module : ModuleBase, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly IExportProfileService _exportProfileService;

        public Module(SmartDbContext db, IExportProfileService exportProfileService)
        {
            _db = db;
            _exportProfileService = exportProfileService;
        }

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "GoogleMerchantCenter", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteLanguageResourcesAsync();

            // Delete existing export profiles.
            var profiles = await _db.ExportProfiles
                .Include(x => x.Deployments)
                .Include(x => x.Task)
                .Where(x => x.ProviderSystemName == GmcXmlExportProvider.SystemName)
                .ToListAsync();

            profiles.Each(async x => await _exportProfileService.DeleteExportProfileAsync(x, true));

            await base.UninstallAsync();
        }
    }
}
