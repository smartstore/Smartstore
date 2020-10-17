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
        //public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
        //        .SetBasePath(Directory.GetCurrentDirectory())
        //        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        //        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
        //        .AddJsonFile("Config/Connections.json", optional: true, reloadOnChange: true)
        //        .AddJsonFile($"Config/Connections.{env.EnvironmentName}.json", optional: true)
        //        .AddEnvironmentVariables()
        //        .Build();

        public static Task Main(string[] args)
            => CreateHostBuilder(args).RunAsync();

        public static IHost CreateHostBuilder(string[] args) 
        {
            // TODO: (core) How to pass the static logger thru app initialization pipeline.
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .CreateLogger();

            try
            {
                Log.Information("Starting up Smartstore");

                return MsHost.CreateDefaultBuilder(args)
                    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                    .ConfigureLogging(SetupLogging)
                    .UseSerilog((hostingContext, configBuilder) =>
                    {
                        configBuilder
                            .ReadFrom.Configuration(hostingContext.Configuration)
                            .Enrich.FromLogContext();
                    }, preserveStaticLogger: true)
                    //.UseSerilog()
                    .ConfigureWebHostDefaults(wb => wb
                        .UseStartup<Startup>(hbc => new Startup(hbc))
                     ).Build();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
                Log.CloseAndFlush();

                return null;
            }
        }


        private static void SetupLogging(HostBuilderContext hostingContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        }
    }
}
