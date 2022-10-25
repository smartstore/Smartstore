using Autofac;
using Microsoft.AspNetCore.Builder;
using Smartstore.Core.Seo;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class SeoStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<UrlService>().As<IUrlService>().InstancePerLifetimeScope();
            builder.RegisterType<XmlSitemapGenerator>().As<IXmlSitemapGenerator>().InstancePerLifetimeScope();
            builder.RegisterType<CanonicalHostUrlFilter>().As<IUrlFilter>().SingleInstance();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.AfterWorkContextMiddleware -4, app =>
            {
                if (builder.ApplicationContext.IsInstalled)
                {
                    app.UseUrlPolicy();
                }
            });
        }
    }
}