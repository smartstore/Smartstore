using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Packaging
{
    /// <summary>
    /// Responsible for installing or uninstalling extension packages (modules or themes)
    /// </summary>
    public partial interface IPackageInstaller
    {
        /// <summary>
        /// Installs an extension package
        /// </summary>
        /// <param name="package">The package to install</param>
        /// <returns>The <see cref="IExtensionDescriptor"/> instance of the deployed extension.</returns>
        Task<IExtensionDescriptor> InstallPackageAsync(ExtensionPackage package);

        /// <summary>
        /// Uninstalls (removes) an extension
        /// </summary>
        /// <param name="extension">The descriptor of extension to remove.</param>
        Task UninstallExtensionAsync(IExtensionDescriptor extension);
    }
}
