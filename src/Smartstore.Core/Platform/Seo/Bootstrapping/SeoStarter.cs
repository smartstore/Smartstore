using System;
using Autofac;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Seo;
using Smartstore.Engine.Builders;
using Smartstore.Engine;

namespace Smartstore.Core.Bootstrapping
{
    internal class SeoStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<UrlService>().As<IUrlService>().InstancePerLifetimeScope();
            builder.Register<UrlPolicy>(x => x.Resolve<IUrlService>().GetUrlPolicy()).InstancePerLifetimeScope();
            builder.RegisterType<XmlSitemapGenerator>().As<IXmlSitemapGenerator>().InstancePerLifetimeScope();
            builder.RegisterType<CanonicalHostUrlFilter>().As<IUrlFilter>().SingleInstance();
        }
    }
}