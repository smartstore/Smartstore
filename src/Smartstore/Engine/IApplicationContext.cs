using System;
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
		/// </summary>
		IModuleCatalog ModuleCatalog { get; }

		/// <summary>
		/// Type scanner used to discover types.
		/// </summary>
		ITypeScanner TypeScanner { get; }

		bool IsInstalled { get; }
		bool IsWebHost { get; }
		string MachineName { get; }
		string EnvironmentIdentifier { get; }

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
	}
}