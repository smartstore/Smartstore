using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;

namespace Smartstore.Core.Installation
{
    /// <summary>
    /// Responsible for installing the application
    /// </summary>
    public partial interface IInstallationService
    {
        Task<InstallationResult> InstallAsync(InstallationModel model, ILifetimeScope scope);

        string GetResource(string resourceName);

        InstallationLanguage GetCurrentLanguage();

        void SaveCurrentLanguage(string languageCode);

        IList<InstallationLanguage> GetInstallationLanguages();

        IEnumerable<InstallationAppLanguageMetadata> GetAppLanguages();

        Lazy<InvariantSeedData, InstallationAppLanguageMetadata> GetAppLanguage(string culture);
    }
}
