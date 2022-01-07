using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Packaging
{
    /// <summary>
    /// Responsible for building extension packages (modules or themes)
    /// </summary>
    public partial interface IPackageBuilder
    {
        /// <summary>
        /// Builds a deployable package for a given extension.
        /// </summary>
        /// <param name="extension">The extension to build a package for.</param>
        /// <returns>The package</returns>
        Task<ExtensionPackage> BuildPackageAsync(IExtensionDescriptor extension);
    }
}
