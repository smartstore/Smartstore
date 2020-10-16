using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Autofac;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Smartstore.Data;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Engine
{
    public class SmartApplicationContext : IApplicationContext, IServiceProviderContainer
    {
        private bool _freezed;
        
        public SmartApplicationContext(IHostEnvironment hostEnvironment, IConfiguration configuration, params Assembly[] coreAssemblies)
        {
            Guard.NotNull(hostEnvironment, nameof(hostEnvironment));
            Guard.NotNull(configuration, nameof(configuration));

            HostEnvironment = hostEnvironment;
            Configuration = configuration;

            ConfigureFileSystem(hostEnvironment);

            DataSettings.SetApplicationContext(this, OnDataSettingsLoaded);

            ModuleCatalog = new ModuleCatalog();
            TypeScanner = new DefaultTypeScanner(ModuleCatalog, coreAssemblies);

            // Create app configuration
            var config = new SmartConfiguration();
            configuration.Bind("Smartstore", config);
            // TODO: (core) Bind ConnectionStrings somehow.
            AppConfiguration = config;
        }

        private void OnDataSettingsLoaded(DataSettings settings)
        {
            this.TenantRoot = settings.TenantRoot;
        }

        private void ConfigureFileSystem(IHostEnvironment hostEnvironment)
        {
            hostEnvironment.ContentRootFileProvider = new LocalFileSystem(hostEnvironment.ContentRootPath);

            if (hostEnvironment is IWebHostEnvironment we)
            {
                we.WebRootFileProvider = new LocalFileSystem(we.WebRootPath);
                WebRoot = (IFileSystem)we.WebRootFileProvider;
            }
            else
            {
                WebRoot = (IFileSystem)hostEnvironment.ContentRootFileProvider;
            }  

            // TODO: (core) Read stuff from config and resolve tenant. Check folders and create them also.
            ThemesRoot = new LocalFileSystem(ContentRoot.MapPath("Themes"));
            ModulesRoot = new LocalFileSystem(ContentRoot.MapPath("Modules"));
            AppDataRoot = new LocalFileSystem(ContentRoot.MapPath("App_Data"));

            if (!AppDataRoot.DirectoryExists("Tenants"))
            {
                AppDataRoot.TryCreateDirectory("Tenants");
            }

            CommonHelper.ContentRoot = ContentRoot;
        }

        IServiceProvider IServiceProviderContainer.ApplicationServices { get; set; }

        public IHostEnvironment HostEnvironment { get; }
        public IConfiguration Configuration { get; }
        public SmartConfiguration AppConfiguration { get; }
        public ITypeScanner TypeScanner { get; }
        public IModuleCatalog ModuleCatalog { get; }

        public ILifetimeScope Services
        {
            get 
            {
                var provider = ((IServiceProviderContainer)this).ApplicationServices;
                return provider?.AsLifetimeScope();
            }
        }

        public bool IsWebHost
        {
            get => HostEnvironment is IWebHostEnvironment;
        }

        public string MachineName => Environment.MachineName;

        // Use the current host and the process id as two servers could run on the same machine
        public string EnvironmentIdentifier => Environment.MachineName + "-" + Environment.ProcessId;

        public IFileSystem ContentRoot => (IFileSystem)HostEnvironment.ContentRootFileProvider;
        public IFileSystem WebRoot { get; private set; }
        public IFileSystem ThemesRoot { get; private set; }
        public IFileSystem ModulesRoot { get; private set; }
        public IFileSystem AppDataRoot { get; private set; }
        public IFileSystem TenantRoot { get; private set; }

        public void Freeze()
        {
            _freezed = true;
        }

        private void CheckFreezed()
        {
            if (_freezed)
            {
                throw new SmartException("Operation invalid after application has been bootstrapped completely.");
            }
        }
    }
}
