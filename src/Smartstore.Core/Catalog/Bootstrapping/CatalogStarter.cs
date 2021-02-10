using Autofac;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Attributes.Modelling;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Platform.Search.Facets;
using Smartstore.Core.Search.Facets;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public class CatalogStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<ProductService>().As<IProductService>().InstancePerLifetimeScope();
            builder.RegisterType<ProductTagService>().As<IProductTagService>().InstancePerLifetimeScope();

            builder.RegisterType<CategoryService>()
                .As<ICategoryService>()
                .As<IXmlSitemapPublisher>()
                .InstancePerLifetimeScope();

            builder.RegisterType<ManufacturerService>()
                .As<IManufacturerService>()
                .As<IXmlSitemapPublisher>()
                //.WithNullCache()  // TODO: (core) Do we really need Autofac registration "WithNullCache"?
                .InstancePerLifetimeScope();

            builder.RegisterType<ProductAttributeMaterializer>().As<IProductAttributeMaterializer>().InstancePerLifetimeScope();
            builder.RegisterType<ProductAttributeService>().As<IProductAttributeService>().InstancePerLifetimeScope();
            builder.RegisterType<ProductAttributeFormatter>().As<IProductAttributeFormatter>().InstancePerLifetimeScope();

            builder.RegisterType<PriceCalculationService>().As<IPriceCalculationService>().InstancePerLifetimeScope();
            builder.RegisterType<PriceFormatter>().As<IPriceFormatter>().InstancePerLifetimeScope();
            builder.RegisterType<StockSubscriptionService>().As<IStockSubscriptionService>().InstancePerLifetimeScope();
            builder.RegisterType<RecentlyViewedProductsService>().As<IRecentlyViewedProductsService>().InstancePerLifetimeScope();
            builder.RegisterType<ProductCompareService>().As<IProductCompareService>().InstancePerLifetimeScope();
            builder.RegisterType<ProductCloner>().As<IProductCloner>().InstancePerLifetimeScope();
            builder.RegisterType<ProductVariantQueryFactory>().As<IProductVariantQueryFactory>().InstancePerLifetimeScope();
            builder.RegisterType<ProductUrlHelper>().InstancePerLifetimeScope();

            // Search.
            builder.RegisterType<CatalogSearchService>().As<ICatalogSearchService>().As<IXmlSitemapPublisher>().InstancePerLifetimeScope();
            builder.RegisterType<LinqCatalogSearchService>().Named<ICatalogSearchService>("linq").InstancePerLifetimeScope();
            builder.RegisterType<CatalogSearchQueryFactory>().As<ICatalogSearchQueryFactory>().InstancePerLifetimeScope();
            builder.RegisterType<CatalogSearchQueryAliasMapper>().As<ICatalogSearchQueryAliasMapper>().InstancePerLifetimeScope();
            builder.RegisterType<CatalogFacetUrlHelper>().As<IFacetUrlHelper>().InstancePerLifetimeScope();
        }
    }
}
