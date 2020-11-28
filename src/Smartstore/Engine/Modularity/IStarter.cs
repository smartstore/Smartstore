using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Collections;

namespace Smartstore.Engine
{
    /// <summary>
    /// An implementation of this interface is used to initialize the services and HTTP request
    /// pipeline of a plugin.
    /// </summary>
    public interface IStarter : ITopologicSortable<string>
    {
        /// <summary>
        /// Get the value to use to order startups to configure services. The default is 0.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Get the value to use to order startups to build the pipeline. The default is the <see cref="Order"/> property.
        /// </summary>
        int ApplicationOrder { get; }

        /// <summary>
        /// Get the value to use to order startups to register the routing endpoints. The default is the <see cref="ApplicationOrder"/> property.
        /// </summary>
        int RoutesOrder { get; }

        /// <summary>
        /// Returns a value indicating whether the starter should run or be skipped. Default is <c>true</c>. 
        /// Override this method if you wish to explicitly allow or suppress starter execution based on some conditions
        /// like application installation state for instance.
        /// </summary>
        /// <param name="appContext">The application context instance.</param>
        /// <returns><c>true</c> if the starter should run, <c>false</c> otherwise.</returns>
        bool Matches(IApplicationContext appContext);

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The collection of service descriptors.</param>
        /// <param name="appContext">The application context instance.</param>
        /// <param name="isActiveModule">
        /// Indicates whether the assembly containing this starter instance is an active (installed) module assembly.
        /// The value is always <c>true</c> if the containing assembly is not a module type.
        /// </param>
        void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule);

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the application's request pipeline.
        /// </summary>
        /// <param name="appContext">The application context instance.</param>
        void ConfigureApplication(IApplicationBuilder builder, IApplicationContext appContext);

        /// <summary>
        /// This method gets called by the runtime. Use this method to register endpoint routes.
        /// </summary>
        /// <param name="appContext">The application context instance.</param>
        void ConfigureRoutes(IApplicationBuilder builder, IEndpointRouteBuilder routes, IApplicationContext appContext);
    }
}