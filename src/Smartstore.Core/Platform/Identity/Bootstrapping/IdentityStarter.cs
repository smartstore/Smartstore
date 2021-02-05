using System;
using Autofac;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Identity;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public sealed class IdentityStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddIdentity<Customer, CustomerRole>()
                .AddDefaultTokenProviders()
                .AddSignInManager();

            // TODO: (core) // Add Identity IEmailSender and ISmsSender to service collection.
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<CustomerService>().As<ICustomerService>().InstancePerLifetimeScope();

            builder.RegisterType<UserStore>().As<IUserStore>().As<IUserStore<Customer>>().InstancePerLifetimeScope();
            builder.RegisterType<RoleStore>().As<IRoleStore>().As<IRoleStore<CustomerRole>>().InstancePerLifetimeScope();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.AfterStaticFilesMiddleware, app =>
            {
                //app.UseIdentity();
            });
        }
    }
}