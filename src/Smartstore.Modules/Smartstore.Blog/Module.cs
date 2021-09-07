using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Blog
{
    internal class Module : ModuleBase, IConfigurable
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "Blog", new { area = "Admin" });

        public override async Task InstallAsync()
        {
            await base.InstallAsync();
            await SaveSettingsAsync<BlogSettings>();
            await ImportLanguageResources();
        }

        public override async Task UninstallAsync()
        {
            await base.UninstallAsync();
            await DeleteSettingsAsync<BlogSettings>();
            await DeleteLanguageResources();
        }
    }
}
