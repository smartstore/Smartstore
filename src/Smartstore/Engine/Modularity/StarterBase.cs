using System;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Engine
{
    /// <inheritdoc />
    public abstract class StarterBase : IStarter, IContainerConfigurer
    {
        /// <inheritdoc />
        public virtual int Order { get; } = 0;

        /// <inheritdoc />
        public virtual int ApplicationOrder => Order;

        /// <inheritdoc />
        public virtual int RoutesOrder => ApplicationOrder;

        /// <inheritdoc />
        public virtual bool Matches(IApplicationContext appContext) => true;

        /// <inheritdoc />
        public virtual void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
        }

        /// <inheritdoc />
        public virtual void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
        }

        /// <inheritdoc />
        public virtual void ConfigureApplication(IApplicationBuilder app, IApplicationContext appContext)
        {
        }

        /// <inheritdoc />
        public virtual void ConfigureRoutes(IApplicationBuilder app, IEndpointRouteBuilder routes, IApplicationContext appContext)
        {
        }
    }
}
