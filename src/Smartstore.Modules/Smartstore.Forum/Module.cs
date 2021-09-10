using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Forum
{
    internal class Module : ModuleBase, IConfigurable
    {
        public static string SystemName => "Smartstore.Forum";

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "Forum", new { area = "Admin" });

        public override async Task InstallAsync()
        {
            await TrySaveSettingsAsync<ForumSettings>();
            await ImportLanguageResources();
            await base.InstallAsync();

            Logger.Info($"Plugin installed: SystemName: {Descriptor.SystemName}, Version: {Descriptor.Version}, Description: '{Descriptor.FriendlyName}'");
        }

        public override async Task UninstallAsync()
        {
            await base.UninstallAsync();
            await DeleteSettingsAsync<ForumSettings>();
            await DeleteLanguageResources();
        }
    }
}
