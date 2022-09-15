global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net.Mime;
global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.OData.Deltas;
global using Microsoft.EntityFrameworkCore;
global using Smartstore.Core.Security;
global using Smartstore.Web.Api.Services;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Web.Api
{
    // TODO: (mg) (core) move methods of generic API controllers (Payments, Uploads) to OData controllers\functions?
    // TODO: (mg) (core) cleanup string resources.
    // TODO: (mg) (core) update API docu https://smartstore.atlassian.net/wiki/spaces/SMNET50/pages/1956121714/Web+API
    internal class Module : ModuleBase, IConfigurable
    {
        public static string SystemName => "Smartstore.WebApi";

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "WebApi", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<WebApiSettings>();
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<WebApiSettings>();
            await DeleteLanguageResourcesAsync();
            await base.UninstallAsync();
        }
    }
}
