using Autofac;

namespace Smartstore.Core.Seo.DependencyInjection
{
    public sealed class SeoModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UrlService>().As<IUrlService>().InstancePerLifetimeScope();
        }
    }
}