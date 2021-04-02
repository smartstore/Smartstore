using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Smartstore.Core.Data;
using Smartstore.Core.Logging.Serilog;
using Smartstore.Engine;
using MsHost = Microsoft.Extensions.Hosting.Host;

namespace Smartstore.Web
{
    public class Program
    {
        private static readonly Regex _rgSystemSource = new Regex("^File|^System|^Microsoft|^Serilog|^Autofac|^Castle|^MiniProfiler|^Newtonsoft|^Pipelines|^StackExchange|^Superpower", RegexOptions.Compiled);
        private static IConfiguration _configuration;

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true)
                .AddJsonFile("Config/connections.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"Config/connections.{EnvironmentName}.json", optional: true)
                .AddJsonFile("Config/usersettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"Config/usersettings.{EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private readonly static string EnvironmentName 
            = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        private readonly static IConfiguration Configuration 
            = _configuration ??= BuildConfiguration();

        public static async Task Main(string[] args)
        {
            var host = BuildWebHost(args);

            // Migrate database
            await MigrateDatabase(host);

            // Run host
            await host.RunAsync();
        }

        private static async Task MigrateDatabase(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SmartDbContext>();
                await db.Database.EnsureCreatedAsync();
            }
        }

        public static IHost BuildWebHost(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // At this stage - after ConfigureServices & ConfigureContainer have been called - we can access IServiceProvider.
            var appContext = host.Services.GetRequiredService<IApplicationContext>();
            var providerContainer = (appContext as IServiceProviderContainer)
                ?? throw new ApplicationException($"The implementation of '${nameof(IApplicationContext)}' must also implement '${nameof(IServiceProviderContainer)}'.");
            providerContainer.ApplicationServices = host.Services;

            // At this stage we can set the scoped service container.
            var engine = host.Services.GetRequiredService<IEngine>();
            engine.Scope = new ScopedServiceContainer(
                host.Services.GetRequiredService<ILifetimeScopeAccessor>(),
                host.Services.GetRequiredService<IHttpContextAccessor>(),
                host.Services.AsLifetimeScope());

            return host;
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = SetupSerilog(Configuration);

            // Create host builder
            var builder = MsHost.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureLogging(SetupLogging)
                .UseSerilog(dispose: true)
                .ConfigureWebHostDefaults(wb => wb
                    .UseStartup<Startup>(hostingContext =>
                    {
                        hostingContext.Configuration = Configuration;
                        var startupLogger = new SerilogLoggerFactory(Log.Logger).CreateLogger("File");
                        return new Startup(hostingContext, startupLogger);
                    }));

            return builder;
        }

        private static void SetupLogging(HostBuilderContext hostingContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        }

        private static Logger SetupSerilog(IConfiguration configuration)
        {
            var builder = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext();

            // Build DEBUG logger
            if (EnvironmentName == "Development")
            {
                builder.WriteTo.Debug();
            }

            builder
                // Build INSTALL logger
                .WriteTo.Conditional(Matching.FromSource("Install"), a => a.Async(logger =>
                {
                    logger.File("App_Data/Logs/install-.log",
                        //restrictedToMinimumLevel: LogEventLevel.Debug,
                        outputTemplate: "{Timestamp:G} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                        fileSizeLimitBytes: 100000000,
                        rollOnFileSizeLimit: true,
                        shared: true,
                        rollingInterval: RollingInterval.Day,
                        flushToDiskInterval: TimeSpan.FromSeconds(5));
                }))
                // Build FILE logger (also replaces the Smartstore classic "TraceLogger")
                .WriteTo.Logger(logger =>
                {
                    logger
                        .Enrich.FromLogContext()
                        // Allow only "File[/{path}]" sources
                        .Filter.ByIncludingOnly(IsFileSource)
                        // Extracts path from source and adds it as log event property name.
                        .Enrich.With<LogFilePathEnricher>()
                        .WriteTo.Map(LogFilePathEnricher.LogFilePathPropertyName, (logFilePath, wt) => 
                        {
                            wt.Async(c => c.File($"{logFilePath}",
                                //restrictedToMinimumLevel: LogEventLevel.Debug,
                                outputTemplate: "{Timestamp:G} [{Level:u3}] {Message:lj} {RequestPath} (UserId: {CustomerId}, Username: {UserName}){NewLine}{Exception}",
                                fileSizeLimitBytes: 100000000,
                                rollOnFileSizeLimit: true,
                                shared: true,
                                rollingInterval: RollingInterval.Day,
                                flushToDiskInterval: TimeSpan.FromSeconds(5)));
                        }, sinkMapCountLimit: 10);
                })
                // Build "SmartDbContext" logger
                .WriteTo.Logger(logger =>
                {
                    logger
                        .Enrich.FromLogContext()
                        // Do not allow system/3rdParty noise less than WRN level
                        .Filter.ByIncludingOnly(IsDbSource)
                        .WriteTo.DbContext(period: TimeSpan.FromSeconds(5), batchSize: 50, eagerlyEmitFirstEvent: false, queueLimit: 1000);
                }, restrictedToMinimumLevel: LogEventLevel.Information, levelSwitch: null);

            return builder.CreateLogger();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDbSource(LogEvent e)
        {
            // Allow only app logs >= INFO or system logs >= WARNING
            return e.Level >= LogEventLevel.Warning || !_rgSystemSource.IsMatch(e.GetSourceContext());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFileSource(LogEvent e)
        {
            var source = e.GetSourceContext();
            return source != null && (source.Equals("File", StringComparison.OrdinalIgnoreCase) || source.StartsWith("File/", StringComparison.OrdinalIgnoreCase));
        }
    }
}
