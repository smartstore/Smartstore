using Autofac;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.DependencyInjection
{
    public sealed class CatalogModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ProductService>().As<IProductService>().InstancePerLifetimeScope();
        }
    }
}
