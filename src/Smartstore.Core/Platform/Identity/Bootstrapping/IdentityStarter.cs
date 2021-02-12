using System;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smartstore.Core.Identity;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Net;

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
                .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
                .AddRoleManager<RoleManager<CustomerRole>>()
                .AddSignInManager<SmartSignInManager>();

            services.AddSingleton<IConfigureOptions<IdentityOptions>, IdentityOptionsConfigurer>();

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = CookieNames.Identity;
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/access-denied";
                options.ReturnUrlParameter = "returnUrl";
            });

            services.ConfigureExternalCookie(options =>
            {
                options.Cookie.Name = CookieNames.ExternalAuthentication;
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/access-denied";
                options.ReturnUrlParameter = "returnUrl";
            });

            // TODO: (core) // Add Identity IEmailSender and ISmsSender to service collection.
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<UserStore>().As<IUserStore>().As<IUserStore<Customer>>().InstancePerLifetimeScope();
            builder.RegisterType<RoleStore>().As<IRoleStore>().As<IRoleStore<CustomerRole>>().InstancePerLifetimeScope();
            builder.RegisterType<CustomerService>().As<ICustomerService>().InstancePerLifetimeScope();
            builder.RegisterType<VoidLookupNormalizer>().As<ILookupNormalizer>().InstancePerLifetimeScope();
            builder.RegisterType<PasswordHasher>().As<IPasswordHasher<Customer>>().InstancePerLifetimeScope();
            builder.RegisterType<UserValidator>().As<IUserValidator<Customer>>().As<IPasswordValidator<Customer>>().InstancePerLifetimeScope();
            builder.RegisterType<GdprTool>().As<IGdprTool>().InstancePerLifetimeScope();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.AuthorizationMiddleware, app =>
            {
                app.UseAuthentication();
                app.UseAuthorization();
            });
        }
    }
}