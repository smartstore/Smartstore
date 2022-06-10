using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Engine
{
    public interface IServiceProviderContainer
    {
        /// <summary>
        /// Gets or sets the ROOT (NOT scoped) application services container.
        /// </summary>
        IServiceProvider ApplicationServices { get; set; }
    }

    public interface IApplicationContext
    {
        /// <summary>
        /// Host environment.
        /// </summary>
        IHostEnvironment HostEnvironment { get; }

        /// <summary>
        /// Root configuration.
        /// </summary>
        IConfiguration Configuration { get; }

        /// <summary>
        /// The root startup logger. Writes to a rolling file in 'App_Data/Logs' and to debug.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Main Smartstore application configuration.
        /// </summary>
        SmartConfiguration AppConfiguration { get; }

        /// <summary>
        /// Provides access to the root application services container.
        /// </summary>
        ILifetimeScope Services { get; }

        /// <summary>
        /// Provides access to module information.
        /// The setter will raise an exception when called after the bootstrapping stage.
        /// </summary>
        IModuleCatalog ModuleCatalog { get; set; }

        /// <summary>
        /// Type scanner used to discover types.
        /// The setter will raise an exception when called after the bootstrapping stage.
        /// </summary>
        ITypeScanner TypeScanner { get; set; }

        /// <summary>
        /// Checks whether the application is fully installed.
        /// </summary>
        bool IsInstalled { get; }

        /// <summary>
        /// Checks whether the current host is a web host.
        /// </summary>
        bool IsWebHost { get; }

        /// <summary>
        /// Gets runtime information about application instance.
        /// </summary>
        RuntimeInfo RuntimeInfo { get; }

        /// <summary>
        /// Gets information about current OS user.
        /// </summary>
        IOSIdentity OSIdentity { get; }

        /// <summary>
        /// Gets a <see cref="IFileSystem"/> pointing at the path that contains application content files.
        /// </summary>
        IFileSystem ContentRoot { get; }

        /// <summary>
        /// Gets a <see cref="IFileSystem"/> pointing at the path that contains web-servable application content files (wwwroot).
        /// </summary>
        IFileSystem WebRoot { get; }

        /// <summary>
        /// Gets a <see cref="IFileSystem"/> pointing at the path that contains all theme directories.
        /// </summary>
        IFileSystem ThemesRoot { get; }

        /// <summary>
        /// Gets a <see cref="IFileSystem"/> pointing at the path that contains all module directories.
        /// </summary>
        IFileSystem ModulesRoot { get; }

        /// <summary>
        /// Gets a <see cref="IFileSystem"/> pointing at the application data root (App_Data)
        /// </summary>
        IFileSystem AppDataRoot { get; }

        /// <summary>
        /// Gets a <see cref="IFileSystem"/> pointing at the path that contains all tenant files (App_Data/Tenants/{Tenant})
        /// </summary>
        IFileSystem TenantRoot { get; }

        /// <summary>
        /// Gets the application temporary directory.
        /// </summary>
        /// <param name="subDirectory">Optional. The relative subdirectory path inside the temporary directory to return instead.</param>
        IDirectory GetTempDirectory(string subDirectory = null);

        /// <summary>
        /// Gets the current tenant's temporary directory.
        /// </summary>
        /// <param name="subDirectory">Optional. The relative subdirectory path inside the temporary directory to return instead.</param>
        IDirectory GetTenantTempDirectory(string subDirectory = null);
    }
}