using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Engine
{
    /// <summary>
    /// Root starter for core application.
    /// </summary>
    public interface IEngineStarter : IDisposable
    {
        /// <summary>
        /// Smartstore configuration
        /// </summary>
        SmartConfiguration AppConfiguration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The collection of service descriptors.</param>
        void ConfigureServices(IServiceCollection services);

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the Autofac container.
        /// </summary>
        /// <param name="builder">The container builder instance.</param>
        void ConfigureContainer(ContainerBuilder builder);

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the application's request pipeline.
        /// </summary>
        void ConfigureApplication(IApplicationBuilder builder);
    }
}
