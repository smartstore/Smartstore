using System.Reflection;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
	/// Module catalog service interface.
	/// </summary>
	public interface IModuleCatalog
    {
        /// <summary> 
        /// Returns a collection of all referenced module assemblies that have been deployed.
        /// </summary>
        IEnumerable<IModuleDescriptor> Modules { get; }

        /// <summary>
        /// Returns a list of all module names which are not compatible with the current application version.
        /// </summary>
        IEnumerable<string> IncompatibleModules { get; }

        /// <summary>
        /// Gets a value indicating whether a module is registered and installed.
        /// </summary>
        /// <param name="systemName">The system name of the module to check for</param>
        /// <returns><c>true</c> if the module exists, <c>false</c> otherwise</returns>
        bool HasModule(string systemName);

        /// <summary>
        /// Gets a module by its main assembly.
        /// </summary>
        /// <param name="assembly">The module assembly</param>
        /// <returns>Descriptor</returns>
        IModuleDescriptor GetModuleByAssembly(Assembly assembly);

        /// <summary>
        /// Gets a module by system name.
        /// </summary>
        /// <param name="name">The module's system name.</param>
        /// <param name="installedOnly">Return the module only if it is installed/loaded.</param>
        /// <returns>Descriptor</returns>
        IModuleDescriptor GetModuleByName(string name, bool installedOnly = true);

        /// <summary>
        /// Gets a companion module by its theme name.
        /// </summary>
        /// <param name="themeName">The theme name.</param>
        /// <param name="installedOnly">Return the module only if it is installed/loaded.</param>
        /// <returns>Descriptor</returns>
        IModuleDescriptor GetModuleByTheme(string themeName, bool installedOnly = true);
    }

    public static class IModuleCatalogExtensions
    {
        /// <summary>
        /// Get descriptors of all installed modules.
        /// </summary>
        /// <returns>Installed module descriptors.</returns>
        public static IEnumerable<IModuleDescriptor> GetInstalledModules(this IModuleCatalog catalog)
        {
            return catalog.Modules.Where(x => x.IsInstalled());
        }
    }
}
