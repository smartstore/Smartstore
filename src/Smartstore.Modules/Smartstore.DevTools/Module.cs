using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.DevTools
{
    internal class Module : ModuleBase, IConfigurable
    {
        private readonly ICommonServices _services;

        public Module(ICommonServices services)
        {
            _services = services;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
        {
            return new RouteInfo("Configure", "DevTools", new { area = "Admin" });
        }

        public override async Task InstallAsync()
        {
            await _services.SettingFactory.SaveSettingsAsync(new ProfilerSettings());
            Logger.Info(string.Format("Plugin installed: SystemName: {0}, Version: {1}, Description: '{2}'", Descriptor.SystemName, Descriptor.Version, Descriptor.FriendlyName));
        }

        public override async Task UninstallAsync()
        {
            await _services.Settings.RemoveSettingsAsync<ProfilerSettings>();
            await _services.DbContext.SaveChangesAsync();
        }
    }
}
