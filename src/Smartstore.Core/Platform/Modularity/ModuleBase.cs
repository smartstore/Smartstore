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
        public virtual Task InstallAsync(ModuleInstallationContext context)
        {
            ModularState.Instance.InstalledModules.Add(Descriptor.SystemName);
            ModularState.Instance.Save();
            context.Logger.Info($"Module installed: SystemName: {Descriptor.SystemName}, Version: {Descriptor.Version}, Description: '{Descriptor.FriendlyName.NaIfEmpty()}'");

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
        protected Task ImportLanguageResourcesAsync()
            => Services.Resolve<IXmlResourceManager>().ImportModuleResourcesFromXmlAsync(Descriptor);

        /// <summary>
        /// Deletes all language resource for the current module if <see cref="IModuleDescriptor.ResourceRootKey"/> is not empty.
        /// </summary>
        protected Task DeleteLanguageResourcesAsync()
        {
            if (Descriptor.ResourceRootKey.IsEmpty())
            {
                return Task.CompletedTask;
            }

            return Services.Localization.DeleteLocaleStringResourcesAsync(Descriptor.ResourceRootKey);
        }

        /// <summary>
        /// Deletes all language resource starting with the given <paramref name="rootKey"/>.
        /// </summary>
        protected Task DeleteLanguageResourcesAsync(string rootKey)
        {
            Guard.NotEmpty(rootKey, nameof(rootKey));
            return Services.Localization.DeleteLocaleStringResourcesAsync(rootKey);
        }

        /// <summary>
        /// Saves the default state of a setting class to the database overwriting any existing value.
        /// </summary>
        /// <returns>The number of inserted or updated setting properties.</returns>
        protected Task<int> SaveSettingsAsync<T>()
             where T : ISettings, new()
        {
            return Services.SettingFactory.SaveSettingsAsync(new T());
        }

        /// <summary>
        /// Saves <paramref name="settings"/> to the database overwriting any existing value.
        /// </summary>
        /// <returns>The number of inserted or updated setting properties.</returns>
        protected Task<int> SaveSettingsAsync<T>(T settings)
             where T : ISettings, new()
        {
            return Services.SettingFactory.SaveSettingsAsync(settings);
        }

        /// <summary>
        /// Saves the default state of a setting class to the database without overwriting existing values.
        /// </summary>
        /// <returns>The number of inserted or updated setting properties.</returns>
        protected Task<int> TrySaveSettingsAsync<T>()
             where T : ISettings, new()
        {
            return SettingFactory.SaveSettingsAsync(Services.DbContext, new T(), false);
        }

        /// <summary>
        /// Saves <paramref name="settings"/> to the database without overwriting existing values.
        /// </summary>
        /// <returns>The number of inserted or updated setting properties.</returns>
        protected Task<int> TrySaveSettingsAsync<T>(T settings)
             where T : ISettings, new()
        {
            return SettingFactory.SaveSettingsAsync(Services.DbContext, settings, false);
        }

        /// <summary>
        /// Deletes all properties from <typeparamref name="T"/> settings from the database.
        /// </summary>
        /// <returns>The number of deleted setting properties.</returns>
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
