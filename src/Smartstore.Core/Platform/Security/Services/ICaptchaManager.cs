#nullable enable

using System.Diagnostics.CodeAnalysis;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Defines methods for managing CAPTCHA providers and their configurations.
    /// </summary>
    /// <remarks>This interface provides functionality to retrieve the current CAPTCHA provider, list all
    /// available providers, and check if CAPTCHA is properly configured.</remarks>
    public interface ICaptchaManager
    {
        /// <summary>
        /// Retrieves the currently active CAPTCHA provider.
        /// </summary>
        /// <returns>The current <see cref="Provider{ICaptchaProvider}"/> instance if one is active; otherwise, <see langword="null"/>.</returns>
        Provider<ICaptchaProvider>? GetCurrentProvider();

        /// <summary>
        /// Retrieves a CAPTCHA provider based on its system name.
        /// </summary>
        /// <param name="systemName">The unique system name of the CAPTCHA provider to retrieve. Cannot be null or empty.</param>
        /// <returns>A <see cref="Provider{ICaptchaProvider}"/> instance representing the CAPTCHA provider if found; otherwise,
        /// <see langword="null"/>.</returns>
        Provider<ICaptchaProvider>? GetProviderBySystemName(string systemName);

        /// <summary>
        /// Retrieves a collection of available CAPTCHA providers.
        /// </summary>
        /// <remarks>Each provider in the collection implements the <see cref="ICaptchaProvider"/>
        /// interface, allowing for CAPTCHA generation and validation. The returned collection may be empty if no
        /// providers are available.</remarks>
        /// <returns>An enumerable collection of <see cref="Provider{T}"/> objects, where each object wraps an <see
        /// cref="ICaptchaProvider"/> implementation.</returns>
        IEnumerable<Provider<ICaptchaProvider>> ListProviders();

        /// <summary>
        /// Retrieves the names of all currently active targets.
        /// </summary>
        /// <returns>An array of strings containing the names of the active targets. 
        /// The array will be empty if no targets are active.</returns>
        string[] GetActiveTargets();

        /// <summary>
        /// Determines whether the specified target is currently active.
        /// </summary>
        /// <param name="target">The name of the target to check. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the specified target is active; otherwise, <see langword="false"/>.</returns>
        bool IsActiveTarget(string target);

        /// <summary>
        /// Determines whether the system is configured with a valid CAPTCHA provider.
        /// </summary>
        /// <remarks>
        /// Use this method to check if a CAPTCHA provider is available and retrieve the current
        /// provider if one is configured.
        /// </remarks>
        /// <param name="currentProvider">
        /// When this method returns, contains the current <see cref="ICaptchaProvider"/> if the system is configured;
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <returns><see langword="true"/> if the system is configured with a valid CAPTCHA provider; otherwise, <see langword="false"/>.</returns>
        bool IsConfigured([NotNullWhen(true)] out Provider<ICaptchaProvider>? currentProvider);
    }
}
