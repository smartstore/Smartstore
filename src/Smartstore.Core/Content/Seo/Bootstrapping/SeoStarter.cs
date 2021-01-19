using System;
using Autofac;
using Smartstore.Core.Content.Seo.Routing;
using Smartstore.Core.Content.Seo;
using Smartstore.Engine.Builders;
using Smartstore.Engine;

namespace Smartstore.Core.Bootstrapping
{
    public class SeoStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<UrlService>().As<IUrlService>().InstancePerLifetimeScope();
            builder.Register<UrlPolicy>(x => x.Resolve<IUrlService>().GetUrlPolicy()).InstancePerLifetimeScope();
            builder.RegisterType<XmlSitemapGenerator>().As<IXmlSitemapGenerator>().InstancePerLifetimeScope();
        }
    }
}