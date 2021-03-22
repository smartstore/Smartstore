using Autofac;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class SearchStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<DefaultIndexManager>().As<IIndexManager>().InstancePerLifetimeScope();
            builder.RegisterType<FacetUrlHelperProvider>().As<IFacetUrlHelperProvider>().InstancePerLifetimeScope();
            builder.RegisterType<FacetTemplateProvider>().As<IFacetTemplateProvider>().InstancePerLifetimeScope();
            builder.RegisterType<DefaultFacetTemplateSelector>().As<IFacetTemplateSelector>().SingleInstance();
        }
    }
}
