global using System;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentValidation;
global using Microsoft.EntityFrameworkCore;
global using Smartstore.Core.Configuration;
global using Smartstore.Engine.Modularity;
global using Smartstore.ShippingByWeight.Domain;
global using Smartstore.ShippingByWeight.Models;
global using Smartstore.Web.Modelling;
using Smartstore.ShippingByWeight.Settings;

namespace Smartstore.ShippingByWeight
{
    internal class Module : ModuleBase
    {
        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await TrySaveSettingsAsync<ShippingByWeightSettings>();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            // TODO: (mh) (core) Flush tables???
            await DeleteLanguageResourcesAsync();
            await DeleteSettingsAsync<ShippingByWeightSettings>();
            await base.UninstallAsync();
        }
    }
}
