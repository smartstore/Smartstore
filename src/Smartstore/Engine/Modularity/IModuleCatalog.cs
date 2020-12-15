using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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
		IEnumerable<ModuleDescriptor> Modules { get; }

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
		/// Gets a value indicating whether the module assembly was properly installed and is active.
		/// </summary>
		/// <param name="assembly">The assembly to check for</param>
		/// <returns><c>true</c> when the assembly is loaded and active.</returns>
		bool IsActiveModuleAssembly(Assembly assembly);

		/// <summary>
		/// Gets a module by assembly.
		/// </summary>
		/// <param name="assembly">The module assembly</param>
		/// <param name="installedOnly">Return the module only if it is installed.</param>
		/// <returns>Descriptor</returns>
		ModuleDescriptor GetModuleByAssembly(Assembly assembly, bool installedOnly = true);

		/// <summary>
		/// Gets a module by system name.
		/// </summary>
		/// <param name="name">The module's system name.</param>
		/// <param name="installedOnly">Return the module only if it is installed.</param>
		/// <returns>Descriptor</returns>
		ModuleDescriptor GetModuleByName(string name, bool installedOnly = true);
	}

	public static class IModuleCatalogExtensions
    {
		/// <summary>
		/// Get descriptors of all installed modules.
		/// </summary>
		/// <returns>Installed module descriptors.</returns>
		public static IEnumerable<ModuleDescriptor> GetInstalledModules(this IModuleCatalog catalog)
		{
			return catalog.Modules.Where(x => x.Installed);
		}

		/// <summary>
		/// Get a module descriptor by type.
		/// </summary>
		/// <returns>Descriptor or <c>null</c>.</returns>
		public static ModuleDescriptor GetModule<TModule>(this IModuleCatalog catalog) where TModule : class, IModule
		{
			return catalog.Modules
				.Where(x => typeof(TModule).IsAssignableFrom(x.ModuleClrType))
				.FirstOrDefault();
		}
	}
}
