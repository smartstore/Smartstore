global using System;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentValidation;
global using Microsoft.EntityFrameworkCore;
global using Smartstore.Core.Configuration;
global using Smartstore.Engine.Modularity;
global using Smartstore.Shipping.Domain;
global using Smartstore.Shipping.Models;
global using Smartstore.Web.Modelling;
using Smartstore.Shipping.Settings;

namespace Smartstore.Shipping
{
    internal class Module : ModuleBase
    {
        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await SaveSettingsAsync<ShippingByTotalSettings>();
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<ShippingByTotalSettings>();
            await DeleteLanguageResourcesAsync();
            await base.UninstallAsync();
        }
    }
}
