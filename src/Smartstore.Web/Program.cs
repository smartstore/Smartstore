using System;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MsHost = Microsoft.Extensions.Hosting.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Smartstore.Web
{
    public class Program
    {
        public static Task Main(string[] args)
            => CreateHostBuilder(args).RunAsync();

        public static IHost CreateHostBuilder(string[] args) 
        {
            var configuration = BuildConfiguration();
            
            // TODO: (core) How to pass the static logger thru app initialization pipeline.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .CreateLogger();

            try
            {
                Log.Information("Starting up Smartstore...");

                return MsHost.CreateDefaultBuilder(args)
                    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                    .ConfigureLogging(SetupLogging)
                    .UseSerilog((hostingContext, configBuilder) =>
                    {
                        hostingContext.Configuration = configuration;
                        configBuilder
                            .ReadFrom.Configuration(configuration)
                            .Enrich.FromLogContext();
                    }, preserveStaticLogger: true)
                    //.UseSerilog()
                    .ConfigureWebHostDefaults(wb => wb
                        .UseStartup<Startup>(hostingContext => 
                        {
                            hostingContext.Configuration = configuration;
                            return new Startup(hostingContext); 
                        })
                     ).Build();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed!");
                Log.CloseAndFlush();

                return null;
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{envName}.json", optional: true)
                .AddJsonFile("Config/Connections.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"Config/Connections.{envName}.json", optional: true)
                .AddJsonFile("Config/Serilog.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"Config/Serilog.{envName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static void SetupLogging(HostBuilderContext hostingContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        }
    }
}
