using System;
using System.Threading.Tasks;
using Smartstore.Core;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;

namespace Smartstore.Engine.Modularity
{
    /// <inheritdoc />
    public abstract class ModuleBase : IModule
    {
        /// <inheritdoc />
        public virtual IModuleDescriptor Descriptor { get; set; }

        protected internal ICommonServices Services { get; set; }

        /// <inheritdoc />
        public virtual Task InstallAsync()
        {
            ModularState.Instance.InstalledModules.Add(Descriptor.SystemName);
            ModularState.Instance.Save();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task UninstallAsync()
        {
            ModularState.Instance.InstalledModules.Remove(Descriptor.SystemName);
            ModularState.Instance.Save();

            return Task.CompletedTask;
        }

        #region Protected helpers

        /// <summary>
        /// Imports all language resources for the current module from xml files in localization directory (if any found).
        /// </summary>
        protected Task ImportLanguageResources()
            => Services.Resolve<IXmlResourceManager>().ImportModuleResourcesFromXmlAsync(Descriptor);

        /// <summary>
        /// Deletes all language resource for the current module if <see cref="IModuleDescriptor.ResourceRootKey"/> is not empty.
        /// </summary>
        /// <returns></returns>
        protected Task DeleteLanguageResources()
        {
            if (Descriptor.ResourceRootKey.IsEmpty())
            {
                return Task.CompletedTask;
            }
            
            return Services.Localization.DeleteLocaleStringResourcesAsync(Descriptor.ResourceRootKey);
        }

        protected Task<int> SaveSettingsAsync<T>()
             where T : ISettings, new()
        {
            return Services.SettingFactory.SaveSettingsAsync(new T());
        }

        protected Task<int> SaveSettingsAsync<T>(T settings)
             where T : ISettings, new()
        {
            return Services.SettingFactory.SaveSettingsAsync(settings);
        }

        protected async Task<int> DeleteSettingsAsync<T>()
             where T : ISettings, new()
        {
            var numDeleted = await Services.Settings.RemoveSettingsAsync<T>();
            await Services.DbContext.SaveChangesAsync();
            return numDeleted;
        }

        #endregion
    }
}
