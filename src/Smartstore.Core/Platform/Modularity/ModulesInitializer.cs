using Microsoft.AspNetCore.Http;
using Smartstore.Core.Localization;
using Smartstore.Engine.Initialization;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Installs pending modules on app startup and checks whether any module has changed
    /// and refreshes all module locale resources.
    /// </summary>
    internal class ModulesInitializer : IApplicationInitializer
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public int Order => int.MinValue + 10;
        public bool ThrowOnError => true;
        public int MaxAttempts => 1;

        public async Task InitializeAsync(HttpContext httpContext)
        {
            var appContext = httpContext.RequestServices.GetRequiredService<IApplicationContext>();
            var resourceManager = httpContext.RequestServices.GetRequiredService<IXmlResourceManager>();

            // Discover and refresh changed module locale resources
            await TryRefreshLocaleResources(appContext.ModuleCatalog, resourceManager);

            // Install pending modules
            var modularState = ModularState.Instance;
            if (modularState.PendingModules.Count == 0)
                return;

            var moduleManager = httpContext.RequestServices.GetRequiredService<ModuleManager>();
            var languageService = httpContext.RequestServices.GetRequiredService<ILanguageService>();
            var processedModules = new List<string>(modularState.PendingModules.Count);
            var exceptions = new List<(string, Exception)>(modularState.PendingModules.Count);

            var installContext = new ModuleInstallationContext
            {
                ApplicationContext = appContext,
                Culture = languageService.GetMasterLanguageSeoCode(),
                Stage = ModuleInstallationStage.ModuleInstallation,
                Logger = Logger
            };

            foreach (var pendingModule in modularState.PendingModules)
            {

                if (modularState.InstalledModules.Contains(pendingModule))
                {
                    processedModules.Add(pendingModule);
                    Logger.Warn("Module '{0}' was marked as pending but is installed already.", pendingModule);
                    continue;
                }

                var descriptor = appContext.ModuleCatalog.GetModuleByName(pendingModule);

                if (descriptor == null)
                {
                    Logger.Warn("Pending module '{0}' is not contained in the module catalog. Skipping installation.", pendingModule);
                }
                else
                {
                    try
                    {
                        var module = moduleManager.CreateInstance(descriptor);
                        installContext.ModuleDescriptor = descriptor;
                        await module.InstallAsync(installContext);
                        Logger.Info("Successfully Installed module '{0}'.", pendingModule);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add((pendingModule, ex));
                    }
                }

                processedModules.Add(pendingModule);
            }

            if (processedModules.Count > 0)
            {
                processedModules.Each(x => modularState.PendingModules.Remove(x));
                modularState.Save();
            }

            if (exceptions.Count > 0)
            {
                var msg = $"Installation of {exceptions.Count} module(s) failed\n=============================================================\n";
                foreach (var item in exceptions)
                {
                    msg += "\n\n" + item.Item1 + "\n-----------------------------------------------------------------\n";
                    msg += item.Item2.ToAllMessages();
                }

                throw new Exception(msg, exceptions[0].Item2);
            }
        }

        public Task OnFailAsync(Exception exception, bool willRetry)
            => Task.CompletedTask;

        private static async Task TryRefreshLocaleResources(IModuleCatalog moduleCatalog, IXmlResourceManager resourceManager)
        {
            var modules = moduleCatalog.GetInstalledModules().ToArray();

            foreach (var module in modules)
            {
                var hasher = resourceManager.CreateModuleResourcesHasher(module);

                if (hasher == null)
                {
                    continue;
                }

                if (hasher.HasChanged)
                {
                    await resourceManager.ImportModuleResourcesFromXmlAsync(module, null, false);
                }
            }
        }
    }
}
