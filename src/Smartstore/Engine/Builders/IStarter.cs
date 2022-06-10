using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Collections;

namespace Smartstore.Engine.Builders
{
    /// <summary>
    /// An implementation of this interface is used to initialize the services, the HTTP request
    /// pipeline and endpoint routes of a module.
    /// </summary>
    public interface IStarter : ITopologicSortable<string>
    {
        /// <summary>
        /// Get the value to use to order startup implementations. The default is 0.
        /// </summary>
        int Order { get; }

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
        void ConfigureServices(IServiceCollection services, IApplicationContext appContext);

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure MVC services.
        /// </summary>
        /// <param name="mvcBuilder">MVC builder.</param>
        /// <param name="services">The collection of service descriptors.</param>
        /// <param name="appContext">The application context instance.</param>
        void ConfigureMvc(IMvcBuilder mvcBuilder, IServiceCollection services, IApplicationContext appContext);

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the application's request pipeline.
        /// Call <see cref="RequestPipelineBuilder.Configure(int, Action{IApplicationBuilder})"/> for each set of
        /// handlers and specify its order by passing any predefined number from <see cref="StarterOrdering"/> (although you could pass any integer).
        /// </summary>
        /// <param name="builder">The pipeline builder instance.</param>
        void BuildPipeline(RequestPipelineBuilder builder);

        /// This method gets called by the runtime. Use this method to register endpoint routes.
        /// Call <see cref="RequestPipelineBuilder.Configure(int, Action{IApplicationBuilder})"/> for each set of
        /// routes and specify its order by passing any predefined number from <see cref="StarterOrdering"/> (although you could pass any integer).
        /// </summary>
        /// <param name="builder">The routing builder instance.</param>
        void MapRoutes(EndpointRoutingBuilder builder);
    }
}