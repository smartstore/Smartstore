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
            => new RouteInfo("Configure", "DevTools", new { area = "Admin" });

        public override async Task InstallAsync()
        {
            await base.InstallAsync();
            await SaveSettingsAsync<ProfilerSettings>();
            await ImportLanguageResources();
            Logger.Info(string.Format("Plugin installed: SystemName: {0}, Version: {1}, Description: '{2}'", Descriptor.SystemName, Descriptor.Version, Descriptor.FriendlyName));
        }

        public override async Task UninstallAsync()
        {
            await base.UninstallAsync();
            await DeleteSettingsAsync<ProfilerSettings>();
            await DeleteLanguageResources();
        }
    }
}
