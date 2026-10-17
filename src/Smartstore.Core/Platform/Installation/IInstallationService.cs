using Autofac;

namespace Smartstore.Core.Installation;

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

    /// <summary>
    /// Gets the installable app languages, discovered from "App_Data/Seed/cultures/{culture}/culture.json".
    /// </summary>
    IEnumerable<InstallationAppLanguageMetadata> GetAppLanguages();

    /// <summary>
    /// Gets the installable app language for <paramref name="culture"/>, or <c>null</c> if there is none.
    /// </summary>
    InstallationAppLanguageMetadata GetAppLanguage(string culture);
}