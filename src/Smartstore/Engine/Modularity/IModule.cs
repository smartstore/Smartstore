using System.Threading.Tasks;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Responsible for installing or uninstalling modules.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Gets or sets the module descriptor
        /// </summary>
        IModuleDescriptor ModuleDescriptor { get; set; }

        /// <summary>
        /// Installs module
        /// </summary>
        Task InstallAsync();

        /// <summary>
        /// Uninstalls module
        /// </summary>
        Task UninstallAsync();
    }
}
