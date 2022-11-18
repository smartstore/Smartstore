using Autofac;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Seo;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class TopicStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<TopicXmlSitemapPublisher>().As<IXmlSitemapPublisher>().InstancePerLifetimeScope();
            builder.RegisterType<TopicWidgetSource>().As<IWidgetSource>().InstancePerLifetimeScope();
        }
    }
}