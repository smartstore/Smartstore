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
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Smartstore;
using Smartstore.Core.Logging.Serilog;
using Smartstore.Utilities;

var rgSystemSource = new Regex("^File|^System|^Microsoft|^Serilog|^Autofac|^Castle|^MiniProfiler|^Newtonsoft|^Pipelines|^Azure|^StackExchange|^Superpower|^Dasync", RegexOptions.Compiled);
var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Production;
var isDevEnvironment = IsDevEnvironment();

// Create the application builder
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = isDevEnvironment ? null : AppContext.BaseDirectory
});

// Add connections.json and usersettings.json to configuration manager
var configuration = (IConfiguration)builder.Configuration
    .AddJsonFile("Config/connections.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"Config/connections.{environmentName}.json", optional: true)
    .AddJsonFile("Config/usersettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"Config/usersettings.{environmentName}.json", optional: true);

// Setup Serilog logging
Log.Logger = SetupSerilog(configuration);

var maxRequestBodySize = configuration["Smartstore:MaxRequestBodySize"];

builder.Host
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureLogging(SetupLogging)
    .UseSerilog(dispose: true);

var startupLogger = new SerilogLoggerFactory(Log.Logger).CreateLogger("File");
var appContext = new SmartApplicationContext(builder.Environment, configuration, startupLogger);
var engine = EngineFactory.Create(appContext.AppConfiguration);
var engineStarter = engine.Start(appContext);

// Configure RequestSizeLimit and RequestFormLimits
if (appContext.AppConfiguration.MaxRequestBodySize != null)
{
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        kestrel.Limits.MaxRequestBodySize = appContext.AppConfiguration.MaxRequestBodySize;
    });

    builder.Services.Configure<FormOptions>(form =>
    {
        form.MultipartBodyLengthLimit = appContext.AppConfiguration.MaxRequestBodySize.Value;
    });
}

// Add NativeLibraryDirectory to PATH environment variable
AddPathToEnv(appContext.RuntimeInfo.NativeLibraryDirectory);

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

// Run application
app.Run();



#region Setup helpers

bool IsDevEnvironment()
{
    if (environmentName == Environments.Development)
        return true;

    if (System.Diagnostics.Debugger.IsAttached)
        return true;

    // if there's a 'Smartstore.sln' in one of the parent folders,
    // then we're likely in a dev environment
    if (CommonHelper.FindSolutionRoot(Directory.GetCurrentDirectory()) != null)
        return true;

    return false;
}

void AddPathToEnv(string path)
{
    var name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Path" : "PATH";
    var value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

    if (value.IsEmpty() || !value.Contains(path))
    {
        value = value.EmptyNull().Trim(';') + ';' + path;
        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
    }
}

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

#endregion

