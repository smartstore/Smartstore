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

		IFileSystem ContentRoot { get; }
		IFileSystem WebRoot { get; }
		IFileSystem ThemesRoot { get; }
		IFileSystem ModulesRoot { get; }
		IFileSystem AppDataRoot { get; }
		IFileSystem TenantRoot { get; }
	}
}