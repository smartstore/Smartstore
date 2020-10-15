using System;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MsHost = Microsoft.Extensions.Hosting.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Smartstore.Web
{
    public class Program
    {
        public static Task Main(string[] args)
            => CreateHostBuilder(args).RunAsync();

        public static IHost CreateHostBuilder(string[] args) =>
            MsHost.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        //.UseContentRoot(Directory.GetCurrentDirectory())
                        //.UseIISIntegration()
                        .UseStartup<Startup>()
                        .ConfigureLogging(SetupLogging);
                })
                .Build();

        private static void SetupLogging(WebHostBuilderContext hostingContext, ILoggingBuilder loggingBuilder)
        {
            // TODO: (core)
        }
    }
}
