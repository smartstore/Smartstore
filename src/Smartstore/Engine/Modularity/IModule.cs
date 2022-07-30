using Microsoft.Extensions.Logging;

namespace Smartstore.Engine.Modularity
{
    public enum ModuleInstallationStage
    {
        /// <summary>
        /// Application is in installation stage.
        /// </summary>
        AppInstallation,

        /// <summary>
        /// Application is installed and bootstrapped. The module should be installed by user request.
        /// </summary>
        ModuleInstallation
    }

    /// <summary>
    /// Module installation context.
    /// </summary>
    public sealed class ModuleInstallationContext
    {
        /// <summary>
        /// The application context.
        /// </summary>
        public IApplicationContext ApplicationContext { get; init; }

        /// <summary>
        /// The descriptor of module currently being installed.
        /// </summary>
        public IModuleDescriptor ModuleDescriptor { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether sample data should be seeded. During 
        /// app installation, reflects the choice the user made in the install wizard.
        /// During module installation value is always <c>null</c>; in this case the module
        /// author should decide whether to seed sample data or not.
        /// </summary>
        public bool? SeedSampleData { get; init; }

        /// <summary>
        /// ISO code of primary installation language.
        /// </summary>
        public string Culture { get; init; }

        /// <summary>
        /// Installation stage.
        /// </summary>
        public ModuleInstallationStage Stage { get; init; }

        /// <summary>
        /// Logger to use.
        /// </summary>
        public ILogger Logger { get; init; }
    }

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
        Task InstallAsync(ModuleInstallationContext context);

        /// <summary>
        /// Executed when a module is uninstalled. This method should remove module specific
        /// data like language resources, settings etc.
        /// </summary>
        Task UninstallAsync();
    }
}
