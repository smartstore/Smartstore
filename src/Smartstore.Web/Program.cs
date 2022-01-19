global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Smartstore.Core;
global using Smartstore.Core.Common;
global using Smartstore.Core.Data;
global using Smartstore.Core.Widgets;
global using Smartstore.Domain;
global using Smartstore.Engine;
global using Smartstore.Web.Components;
global using Smartstore.Web.Controllers;
global using Smartstore.Web.Filters;
global using Smartstore.Web.Modelling;
global using EntityState = Smartstore.Data.EntityState;

using System.Text.RegularExpressions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Smartstore;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Logging.Serilog;

var rgSystemSource = new Regex("^File|^System|^Microsoft|^Serilog|^Autofac|^Castle|^MiniProfiler|^Newtonsoft|^Pipelines|^StackExchange|^Superpower", RegexOptions.Compiled);
var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Production;
//var configuration = BuildConfiguration();

// Create the application builder
var builder = WebApplication.CreateBuilder(args);

// Add connections.json and usersettings.json to configuration manager
var configuration = (IConfiguration)builder.Configuration
    .AddJsonFile("Config/connections.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"Config/connections.{environmentName}.json", optional: true)
    .AddJsonFile("Config/usersettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"Config/usersettings.{environmentName}.json", optional: true);

// Setup Serilog logging
Log.Logger = SetupSerilog(configuration);

// Configure the host
builder.Host
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureLogging(SetupLogging)
    .UseSerilog(dispose: true);

var startupLogger = new SerilogLoggerFactory(Log.Logger).CreateLogger("File");
var appContext = new SmartApplicationContext(builder.Environment, configuration, startupLogger);
var engine = EngineFactory.Create(appContext.AppConfiguration);
var engineStarter = engine.Start(appContext);

// Add services to the container.
engineStarter.ConfigureServices(builder.Services);

// Add services to the Autofac container.
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    engineStarter.ConfigureContainer(container);
});

// Build the application
var app = builder.Build();

// At this stage we can access IServiceProvider.
var providerContainer = appContext as IServiceProviderContainer;
providerContainer.ApplicationServices = app.Services;

// At this stage we can set the scoped service container.
engine.Scope = new ScopedServiceContainer(
    app.Services.GetRequiredService<ILifetimeScopeAccessor>(),
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Services.AsLifetimeScope());

// Build request pipeline
app.Lifetime.ApplicationStarted.Register(() => 
{
    appContext.Freeze();
    engineStarter.Dispose();
    engineStarter = null;
});
engineStarter.ConfigureApplication(app);

// Initialize databases
await InitializeDatabasesAsync();

// Run application
app.Run();

#region Setup helpers

void SetupLogging(ILoggingBuilder loggingBuilder)
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog();
}

Logger SetupSerilog(IConfiguration configuration)
{
    var builder = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext();

    // Build DEBUG logger
    if (environmentName == "Development")
    {
        builder.WriteTo.Debug();
    }

    var strDbMinLevel = configuration["Serilog:MinimumLevel:Database"];
    var dbMinLevel = strDbMinLevel.HasValue()
        ? (LogEventLevel)Enum.Parse(typeof(LogEventLevel), strDbMinLevel)
        : LogEventLevel.Information;

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
        }, restrictedToMinimumLevel: dbMinLevel, levelSwitch: null);

    return builder.CreateLogger();
}

bool IsDbSource(LogEvent e)
{
    // Allow only app logs >= INFO or system logs >= WARNING
    return e.Level >= LogEventLevel.Warning || !rgSystemSource.IsMatch(e.GetSourceContext());
}

bool IsFileSource(LogEvent e)
{
    var source = e.GetSourceContext();
    return source != null && (source.Equals("File", StringComparison.OrdinalIgnoreCase) || source.StartsWith("File/", StringComparison.OrdinalIgnoreCase));
}

async Task InitializeDatabasesAsync()
{
    if (appContext.IsInstalled)
    {
        var scopeAccessor = app.Services.GetRequiredService<ILifetimeScopeAccessor>();
        using (scopeAccessor.BeginContextAwareScope(out var scope))
        {
            var initializer = scope.ResolveOptional<IDatabaseInitializer>();
            if (initializer != null)
            {
                await initializer.InitializeDatabasesAsync(app.Lifetime?.ApplicationStopping ?? CancellationToken.None);
            }
        }
    }
}

#endregion

#region Legacy

//// Create host builder
//builder.WebHost
//    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
//    .ConfigureLogging(SetupLogging)
//    .UseSerilog(dispose: true)
//    .ConfigureWebHostDefaults(wb => wb
//        //.UseWebRoot("wwwroot")
//        .UseStartup(hostingContext =>
//        {
//            hostingContext.Configuration = Configuration;

//            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            {
//                // TODO: (core) Temp only
//                hostingContext.HostingEnvironment.ContentRootPath = AppContext.BaseDirectory;
//                hostingContext.HostingEnvironment.WebRootPath = Path.Combine(hostingContext.HostingEnvironment.ContentRootPath, "wwwroot");
//            }

//            var startupLogger = new SerilogLoggerFactory(Log.Logger).CreateLogger("File");
//            return new Startup(hostingContext, startupLogger);
//        }));

#endregion
