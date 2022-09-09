global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net.Mime;
global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.OData.Deltas;
global using Microsoft.EntityFrameworkCore;
global using Smartstore.WebApi.Services;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.WebApi
{
    // TODO: (mg) (core) use basic authentication over HTTPS, as it is recommended by OData Protocol Version 4.0.
    // https://docs.microsoft.com/en-us/odata/webapi/basic-auth
    // TODO: (mg) (core) move methods of generic API controllers (Payments, Uploads) to OData controllers\functions?
    // TODO: (mg) (core) cleanup string resources.
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
