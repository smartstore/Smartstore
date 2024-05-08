namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Provides metadata about a module.
    /// </summary>
    public interface IModuleDescriptor : IExtensionDescriptor, IExtensionLocation
    {
        /// <summary>
        /// Gets the system name
        /// </summary>
        string SystemName { get; }

        /// <summary>
        /// Gets the module system names the module depends on (or <c>null</c>)
        /// </summary>
        string[] DependsOn { get; }

        /// <summary>
        /// Gets the (display) order
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Gets a value indicating whether the module is incompatible with the current application version.
        /// </summary>
        bool Incompatible { get; }

        /// <summary>
        /// Gets the root key of string resources.
        /// </summary>
        /// <remarks>
        /// Tries to get it from first entry of resource XML file if not specified.
        /// In that case the first resource name should not contain a dot if it's not part of the root key.
        /// Otherwise you get the wrong root key.
        /// </remarks>
        string ResourceRootKey { get; }

        /// <summary>
        /// Gets the file name of the brand image in the module's 'wwwroot' directory (without path).
        /// Searched file names are: "branding.png", "branding.gif", "branding.jpg", "branding.jpeg".
        /// Returns an empty string if no such file can be found.
        /// </summary>
        string BrandImageFileName { get; }

        /// <summary>
        /// Builds a setting key. Pattern: "PluginSetting.{ModuleSystemName}.{SettingName}"
        /// </summary>
        string GetSettingKey(string name);

        /// <summary>
        /// Gets the main assembly file name.
        /// </summary>
        /// <remarks>
        string AssemblyName { get; }

        /// <summary>
        /// The full physical path of the module source code directory if running in dev mode, <c>null</c> otherwise.
        /// </summary>
        string SourcePhysicalPath { get; }

        /// <summary>
        /// Theme name (if the module is a theme companion). On build, a symbolic link will be created in /Themes/[Theme] to /Modules/[Module].
        /// </summary>
        string Theme { get; }

        /// <summary>
        /// Gets the module's runtime assembly info.
        /// </summary>
        ModuleAssemblyInfo Module { get; }
    }

    public static class IModuleDescriptorExtensions
    {
        /// <summary>
        /// Checks whether the module is installed and loaded.
        /// </summary>
        public static bool IsInstalled(this IModuleDescriptor descriptor)
            => Guard.NotNull(descriptor).Module?.Assembly != null;
    }
}
