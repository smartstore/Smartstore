using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine.Initialization;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Installs pending modules on app startup.
    /// </summary>
    internal class PendingModulesInitializer : IApplicationInitializer
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public int Order => int.MinValue;
        public bool ThrowOnError => true;
        public int MaxAttempts => 1;

        public async Task InitializeAsync(HttpContext httpContext)
        {
            var modularState = ModularState.Instance;
            if (modularState.PendingModules.Count == 0)
                return;

            var moduleCatalog = httpContext.RequestServices.GetRequiredService<IModuleCatalog>();
            var moduleManager = httpContext.RequestServices.GetRequiredService<ModuleManager>();
            var processedModules = new List<string>(modularState.PendingModules.Count);
            var exceptions = new List<(string, Exception)>(modularState.PendingModules.Count);

            foreach (var pendingModule in modularState.PendingModules)
            {
                if (modularState.InstalledModules.Contains(pendingModule))
                {
                    processedModules.Add(pendingModule);
                    Logger.Warn("Module '{0}' was marked as pending but is installed already.", pendingModule);
                    continue;
                }
                
                var descriptor = moduleCatalog.GetModuleByName(pendingModule);

                if (descriptor == null)
                {
                    Logger.Warn("Pending module '{0}' is not contained in the module catalog. Skipping installation.", pendingModule);
                }
                else
                {
                    try
                    {
                        var module = moduleManager.CreateInstance(descriptor);
                        await module.InstallAsync();
                        Logger.Info("Successfully Installed module '{0}'.", pendingModule);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Installation of module '{0}' failed.", pendingModule);
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
                var msg = $"Installation of {exceptions.Count} module(s) failed\n===================================================\n";
                foreach (var item in exceptions)
                {
                    msg += "\n\n" + item.Item1 + "\n-----------------------------------------------------------------\n";
                    msg += item.Item2.ToAllMessages();
                }

                throw new SmartException(msg, exceptions[0].Item2);
            }
        }

        public Task OnFailAsync(Exception exception, bool willRetry)
            => Task.CompletedTask;
    }
}
