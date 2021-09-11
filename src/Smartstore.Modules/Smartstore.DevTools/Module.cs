using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.DevTools
{
    internal class Module : ModuleBase, IConfigurable
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "DevTools", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<ProfilerSettings>();
            await ImportLanguageResources();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<ProfilerSettings>();
            await DeleteLanguageResources();
            await base.UninstallAsync();
        }
    }
}
