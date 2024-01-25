using Autofac;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Attributes.Modelling;
using Smartstore.Core.Catalog.Attributes.Rules;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Brands.Rules;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Categories.Rules;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Products.Rules;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Seo;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class CatalogStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
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
                .InstancePerLifetimeScope();

            builder.RegisterType<ProductAttributeMaterializer>().As<IProductAttributeMaterializer>().InstancePerLifetimeScope();
            builder.RegisterType<ProductAttributeService>().As<IProductAttributeService>().InstancePerLifetimeScope();
            builder.RegisterType<ProductAttributeFormatter>().As<IProductAttributeFormatter>().InstancePerLifetimeScope();

            builder.RegisterType<DiscountService>().As<IDiscountService>().InstancePerLifetimeScope();
            builder.RegisterType<StockSubscriptionService>().As<IStockSubscriptionService>().InstancePerLifetimeScope();
            builder.RegisterType<RecentlyViewedProductsService>().As<IRecentlyViewedProductsService>().InstancePerLifetimeScope();
            builder.RegisterType<ProductCompareService>().As<IProductCompareService>().InstancePerLifetimeScope();
            builder.RegisterType<ProductCloner>().As<IProductCloner>().InstancePerLifetimeScope();
            builder.RegisterType<ProductVariantQueryFactory>().As<IProductVariantQueryFactory>().InstancePerLifetimeScope();
            builder.RegisterType<ProductUrlHelper>().InstancePerLifetimeScope();
            builder.RegisterType<ProductImporter>().Keyed<IEntityImporter>(ImportEntityType.Product).InstancePerLifetimeScope();
            builder.RegisterType<CategoryImporter>().Keyed<IEntityImporter>(ImportEntityType.Category).InstancePerLifetimeScope();

            // Search.
            builder.RegisterType<CatalogSearchQueryVisitor>()
                .As<LinqSearchQueryVisitor<Product, CatalogSearchQuery, CatalogSearchQueryContext>>()
                .SingleInstance();

            builder.RegisterType<CatalogSearchService>().As<ICatalogSearchService>().As<IXmlSitemapPublisher>().InstancePerLifetimeScope();
            builder.RegisterType<LinqCatalogSearchService>().Named<ICatalogSearchService>("linq").InstancePerLifetimeScope();
            builder.RegisterType<CatalogSearchQueryFactory>().As<ICatalogSearchQueryFactory>().InstancePerLifetimeScope();
            builder.RegisterType<CatalogSearchQueryAliasMapper>().As<ICatalogSearchQueryAliasMapper>().InstancePerLifetimeScope();
            builder.RegisterType<CatalogFacetUrlHelper>().As<IFacetUrlHelper>().InstancePerLifetimeScope();

            // Pricing
            builder.RegisterType<PriceCalculationService>().As<IPriceCalculationService>().InstancePerLifetimeScope();
            builder.RegisterType<PriceCalculatorFactory>().As<IPriceCalculatorFactory>().InstancePerLifetimeScope();
            builder.RegisterType<PriceLabelService>().As<IPriceLabelService>().InstancePerLifetimeScope();

            DiscoverCalculators(builder, appContext);

            // Rules.
            builder.RegisterType<ProductRuleProvider>().As<IProductRuleProvider>().InstancePerLifetimeScope();
            builder.RegisterType<NullAttributeRuleProvider>().As<IAttributeRuleProvider>().InstancePerLifetimeScope();

            // Rule options provider.
            builder.RegisterType<ProductVariantAttributeValueRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<SpecificationAttributeOptionRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<ManufacturerRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<CategoryRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<ProductRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<ProductTagRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        }

        private static void DiscoverCalculators(ContainerBuilder builder, IApplicationContext appContext)
        {
            var calculatorTypes = appContext.TypeScanner.FindTypes<IPriceCalculator>();

            foreach (var calculatorType in calculatorTypes)
            {
                var usageAttribute = calculatorType.GetAttribute<CalculatorUsageAttribute>(true);

                var registration = builder
                    .RegisterType(calculatorType)
                    .As<IPriceCalculator>()
                    .Keyed<IPriceCalculator>(calculatorType)
                    .InstancePerAttributedLifetime()
                    .WithMetadata<PriceCalculatorMetadata>(m =>
                    {
                        m.For(em => em.CalculatorType, calculatorType);
                        m.For(em => em.ValidTargets, usageAttribute?.ValidTargets ?? CalculatorTargets.All);
                        m.For(em => em.Order, usageAttribute?.Order ?? CalculatorOrdering.Default);
                    });
            }

            // Register calculator resolve delegate
            builder.Register<Func<Type, IPriceCalculator>>(c =>
            {
                var cc = c.Resolve<IComponentContext>();
                return key => cc.ResolveKeyed<IPriceCalculator>(key);
            });
        }
    }
}
