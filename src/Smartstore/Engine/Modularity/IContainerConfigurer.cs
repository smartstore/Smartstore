using System;
using Autofac;

namespace Smartstore.Engine
{
    public interface IContainerConfigurer
    {
        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the Autofac container.
        /// </summary>
        /// <param name="builder">The container builder instance.</param>
        /// <param name="appContext">The application context instance.</param>
        /// <param name="isActiveModule">
        /// Indicates whether the assembly containing this configurer instance is an active (installed) plugin assembly.
        /// The value is always <c>true</c> if the containing assembly is not a plugin type.
        /// </param>
        void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule);
    }
}
