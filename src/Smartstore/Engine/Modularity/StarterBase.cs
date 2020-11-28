using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Collections;

namespace Smartstore.Engine
{
    /// <inheritdoc />
    public abstract class StarterBase : IStarter, IContainerConfigurer
    {
        private HashSet<string> _runAfter = new();
        
        /// <inheritdoc />
        public virtual int Order { get; } = (int)StarterOrdering.Default;

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

        /// <summary>
        /// Instructs this starter implementation to run after <typeparamref name="T"/> on equal sort order.
        /// </summary>
        protected void RunAfter<T>() where T : IStarter, new()
            => _runAfter.Add(typeof(T).FullName);

        /// <summary>
        /// Instructs this starter implementation to run after <paramref name="starterTypeFullName"/> on equal sort order.
        /// Pass the full name of the starter type (qualified with namespace, but without assembly part)
        /// </summary>
        protected void RunAfter(string starterTypeFullName)
        {
            Guard.NotEmpty(starterTypeFullName, nameof(starterTypeFullName));
            _runAfter.Add(starterTypeFullName);
        }

        string ITopologicSortable<string>.Key
            => GetType().FullName;

        string[] ITopologicSortable<string>.DependsOn 
            => _runAfter.ToArray();
    }
}
