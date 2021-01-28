using Autofac;
using Smartstore.Core.Platform.Search.Facets;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public class SearchStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<FacetHelperProvider>().As<IFacetHelperProvider>().InstancePerLifetimeScope();
        }
    }
}
