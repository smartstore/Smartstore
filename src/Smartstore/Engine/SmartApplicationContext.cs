using System.Runtime.CompilerServices;
using Autofac;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Smartstore.Data;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Engine
{
    public class SmartApplicationContext : IApplicationContext, IServiceProviderContainer
    {
        const string TempDirName = "_temp";

        private bool _freezed;
        private IModuleCatalog _moduleCatalog;
        private ITypeScanner _typeScanner;
        private IDirectory _tempDirectory;
        private IDirectory _tempDirectoryTenant;

        public SmartApplicationContext(IHostEnvironment hostEnvironment, IConfiguration configuration, ILogger logger)
        {
            Guard.NotNull(hostEnvironment, nameof(hostEnvironment));
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));

            HostEnvironment = hostEnvironment;
            Configuration = configuration;
            Logger = logger;
            RuntimeInfo = new RuntimeInfo(hostEnvironment);

            ConfigureFileSystem(hostEnvironment);
            DataSettings.SetApplicationContext(this, OnDataSettingsLoaded);

            // Create app configuration
            // TODO: (core) Try to incorporate IOptionsMonitor<SmartConfiguration> somehow.
            var config = new SmartConfiguration();
            configuration.Bind("Smartstore", config);

            AppConfiguration = config;
        }

        private void OnDataSettingsLoaded(DataSettings settings)
        {
            TenantRoot = settings.TenantRoot;
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
            if (ContentRoot.DirectoryExists("Modules"))
            {
                ModulesRoot = new ExpandedFileSystem("Modules", ContentRoot);
            }

            if (ContentRoot.DirectoryExists("Themes"))
            {
                ThemesRoot = new ExpandedFileSystem("Themes", ContentRoot);
            }

            if (ContentRoot.DirectoryExists("App_Data"))
            {
                AppDataRoot = new ExpandedFileSystem("App_Data", ContentRoot);

                if (!AppDataRoot.DirectoryExists("Tenants"))
                {
                    AppDataRoot.TryCreateDirectory("Tenants");
                }
            }

            CommonHelper.ContentRoot = ContentRoot;
            WebHelper.WebRoot = WebRoot;
        }

        IServiceProvider IServiceProviderContainer.ApplicationServices { get; set; }

        public IModuleCatalog ModuleCatalog
        {
            get => _moduleCatalog;
            set
            {
                CheckFreezed();
                _moduleCatalog = Guard.NotNull(value, nameof(value));
            }
        }

        public ITypeScanner TypeScanner
        {
            get => _typeScanner;
            set
            {
                CheckFreezed();
                _typeScanner = Guard.NotNull(value, nameof(value));
            }
        }

        public IHostEnvironment HostEnvironment { get; }
        public IConfiguration Configuration { get; }
        public ILogger Logger { get; }
        public SmartConfiguration AppConfiguration { get; }

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

        public bool IsInstalled
        {
            get => DataSettings.DatabaseIsInstalled();
        }

        public RuntimeInfo RuntimeInfo { get; }
        public IOSIdentity OSIdentity { get; } = new GenericOSIdentity();
        public IFileSystem ContentRoot => (IFileSystem)HostEnvironment.ContentRootFileProvider;
        public IFileSystem WebRoot { get; private set; }
        public IFileSystem ThemesRoot { get; private set; }
        public IFileSystem ModulesRoot { get; private set; }
        public IFileSystem AppDataRoot { get; private set; }
        public IFileSystem TenantRoot { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDirectory GetTempDirectory(string subDirectory = null)
        {
            return GetTempDirectoryInternal(AppDataRoot, ref _tempDirectory, subDirectory);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDirectory GetTenantTempDirectory(string subDirectory = null)
        {
            return GetTempDirectoryInternal(TenantRoot, ref _tempDirectoryTenant, subDirectory);
        }

        private static IDirectory GetTempDirectoryInternal(IFileSystem fs, ref IDirectory directory, string subDirectory)
        {
            if (directory == null)
            {
                fs.TryCreateDirectory(TempDirName);
                Interlocked.Exchange(ref directory, fs.GetDirectory(TempDirName));
            }

            if (subDirectory.HasValue())
            {
                var subdir = fs.GetDirectory(PathUtility.Join(TempDirName, subDirectory));
                subdir.Create();
                return subdir;
            }
            else
            {
                return directory;
            }
        }

        public void Freeze() => _freezed = true;
        private void CheckFreezed()
        {
            if (_freezed)
                throw new InvalidOperationException("Operation invalid after application has been bootstrapped completely.");
        }
    }
}
