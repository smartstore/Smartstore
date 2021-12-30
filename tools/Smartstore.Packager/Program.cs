using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine;
using Smartstore.IO;
using System.Diagnostics;

namespace Smartstore.Packager
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var environment = new HostEnvironment
            {
                ApplicationName = "Smartstore Packager",
                EnvironmentName = Debugger.IsAttached ? Environments.Development : Environments.Production,
                ContentRootPath = Directory.GetCurrentDirectory(),
                ContentRootFileProvider = new LocalFileSystem(Directory.GetCurrentDirectory())
            };

            var appContext = new SmartApplicationContext(environment, configuration, NullLogger.Instance);
            appContext.AppConfiguration.EngineType = typeof(SimpleEngine).AssemblyQualifiedName;

            EngineFactory
                .Create(appContext.AppConfiguration)
                .Start(appContext)
                .Dispose();

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }

        class HostEnvironment : IHostEnvironment
        {
            public string ApplicationName { get; set; }
            public string EnvironmentName { get; set; }
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
        }
    }
}