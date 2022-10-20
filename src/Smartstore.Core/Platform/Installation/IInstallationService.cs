using Autofac;

namespace Smartstore.Core.Installation
{
    /// <summary>
    /// Thrown when application is installed already or is currently running (hast started but not completed yet).
    /// </summary>
    public class InstallationException : Exception
    {
        public InstallationException(string message, InstallationResult result)
            : base(message)
        {
            Result = Guard.NotNull(result, nameof(result));
        }

        public InstallationResult Result { get; }
    }

    /// <summary>
    /// Responsible for installing the application
    /// </summary>
    public partial interface IInstallationService
    {
        Task<InstallationResult> InstallAsync(InstallationModel model, ILifetimeScope scope, CancellationToken cancelToken = default);

        InstallationResult GetCurrentInstallationResult();

        string GetResource(string resourceName);

        InstallationLanguage GetCurrentLanguage();

        void SaveCurrentLanguage(string languageCode);

        IList<InstallationLanguage> GetInstallationLanguages();

        IEnumerable<InstallationAppLanguageMetadata> GetAppLanguages();

        Lazy<InvariantSeedData, InstallationAppLanguageMetadata> GetAppLanguage(string culture);
    }
}
