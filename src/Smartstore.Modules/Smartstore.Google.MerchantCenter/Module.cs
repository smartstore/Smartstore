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
            var exportProfiles = await _db.ExportProfiles
                .Include(x => x.Deployments)
                .Include(x => x.Task)
                .Where(x => x.ProviderSystemName == GmcXmlExportProvider.SystemName)
                .ToListAsync();

            foreach (var exportProfile in exportProfiles)
            {
                await _exportProfileService.DeleteExportProfileAsync(exportProfile, true);
            }

            await base.UninstallAsync();
        }
    }
}
