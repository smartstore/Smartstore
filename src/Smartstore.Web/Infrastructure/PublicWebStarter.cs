using System;
using Autofac;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data.Migrations;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Controllers;
using Smartstore.Web.Infrastructure.Installation;

namespace Smartstore.Web.Infrastructure
{
    internal class PublicWebStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            if (appContext.IsInstalled)
            {
                builder.RegisterType<CatalogHelper>().InstancePerLifetimeScope();
                builder.RegisterType<OrderHelper>().InstancePerLifetimeScope();
            }
            else
            {
                // Installation dependencies
                
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
        }
    }
}
