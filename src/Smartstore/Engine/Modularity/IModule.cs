using System.Threading.Tasks;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Responsible for installing or uninstalling modules.
    /// Implementations are auto-discovered on app startup and registered as transient
    /// in service container as <see cref="IModule"/> and also as concrete type.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Gets or sets the module descriptor
        /// </summary>
        IModuleDescriptor Descriptor { get; set; }

        /// <summary>
        /// Executed when a module is installed. This method should perform
        /// common data seeding tasks like importing language resources, saving initial settings data etc.
        /// </summary>
        Task InstallAsync();

        /// <summary>
        /// Executed when a module is uninstalled. This method should remove module specific
        /// data like language resources, settings etc.
        /// </summary>
        Task UninstallAsync();
    }
}
