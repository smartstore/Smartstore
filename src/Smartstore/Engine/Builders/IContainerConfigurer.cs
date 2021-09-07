using Autofac;

namespace Smartstore.Engine.Builders
{
    public interface IContainerConfigurer
    {
        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the Autofac container.
        /// </summary>
        /// <param name="builder">The container builder instance.</param>
        /// <param name="appContext">The application context instance.</param>
        void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext);
    }
}
