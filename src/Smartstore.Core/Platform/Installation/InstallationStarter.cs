using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Installation
{
    internal sealed class InstallationStarter : StarterBase
    {
        const string InstallPath = "/install";

        public override bool Matches(IApplicationContext appContext)
            => !appContext.IsInstalled;

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<InstallationService>().As<IInstallationService>().InstancePerLifetimeScope();

            // Register app languages for installation
            builder.RegisterType<EnUSSeedData>()
                .As<InvariantSeedData>()
                .WithMetadata<InstallationAppLanguageMetadata>(m =>
                {
                    m.For(em => em.Culture, "en-US");
                    m.For(em => em.Name, "English");
                    m.For(em => em.UniqueSeoCode, "en");
                    m.For(em => em.FlagImageFileName, "us.png");
                })
                .InstancePerLifetimeScope();
            builder.RegisterType<DeDESeedData>()
                .As<InvariantSeedData>()
                .WithMetadata<InstallationAppLanguageMetadata>(m =>
                {
                    m.For(em => em.Culture, "de-DE");
                    m.For(em => em.Name, "Deutsch");
                    m.For(em => em.UniqueSeoCode, "de");
                    m.For(em => em.FlagImageFileName, "de.png");
                })
                .InstancePerLifetimeScope();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.EarlyMiddleware, app =>
            {
                app.Use(async (context, next) =>
                {
                    if (!context.Request.Path.StartsWithSegments(InstallPath))
                    {
                        context.Response.Redirect(context.Request.PathBase.Value + InstallPath);
                        return;
                    }

                    await next();
                });
            });
        }
    }
}
