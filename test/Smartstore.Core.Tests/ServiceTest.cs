using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Extensions.Logging;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Pricing.Calculators;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Tests.Catalog.Pricing.Calculators;
using Smartstore.Core.Tests.Checkout.Payment;
using Smartstore.Core.Tests.Common;
using Smartstore.Core.Tests.Shipping;
using Smartstore.Core.Tests.Tax;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;
using Smartstore.Test.Common;
using Smartstore.Utilities;

namespace Smartstore.Core.Tests
{
    [TestFixture]
    public abstract class ServiceTest
    {
        private MockProviderManager _providerManager = new();
        private SmartDbContext _dbContext;
        private IEngine _engine;
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Wrong analysis")]
        private ILifetimeScope _lifetimeScope;
        private IDisposable _lifetimeToken;

        protected MockProviderManager ProviderManager
            => _providerManager;

        protected SmartDbContext DbContext
            => _dbContext;

        protected IEngine Engine
            => _engine;

        protected ILifetimeScope LifetimeScope
            => _lifetimeScope;

        [OneTimeSetUp]
        public void SetUp()
        {
            CommonHelper.IsHosted = false;
            CommonHelper.IsDevEnvironment = false;

            var host = CreateHostBuilder().Build();

            var appContext = host.Services.GetRequiredService<IApplicationContext>();
            var providerContainer = (appContext as IServiceProviderContainer)
                ?? throw new ApplicationException($"The implementation of '${nameof(IApplicationContext)}' must also implement '${nameof(IServiceProviderContainer)}'.");
            providerContainer.ApplicationServices = host.Services;

            var engine = host.Services.GetRequiredService<IEngine>();
            var scopeAccessor = host.Services.GetRequiredService<ILifetimeScopeAccessor>();
            engine.Scope = new ScopedServiceContainer(
                scopeAccessor,
                host.Services.GetRequiredService<IHttpContextAccessor>(),
                host.Services.AsLifetimeScope());

            _engine = engine;

            // Initialize memory DbContext
            InitDbContext();

            InitProviders();

            _lifetimeToken = scopeAccessor.BeginContextAwareScope(out _lifetimeScope);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _lifetimeToken.Dispose();
            _dbContext.Dispose();
        }

        private IHostBuilder CreateHostBuilder()
        {
            IEngineStarter starter = null;

            var builder = Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddEnvironmentVariables();
                })
                .ConfigureHostOptions((context, options) =>
                {
                    context.HostingEnvironment.EnvironmentName = Debugger.IsAttached ? Environments.Development : Environments.Production;
                })
                .ConfigureServices((context, services) =>
                {
                    var appContext = new SmartApplicationContext(
                        context.HostingEnvironment,
                        context.Configuration,
                        NullLogger.Instance);

                    appContext.AppConfiguration.EngineType = typeof(TestEngine).AssemblyQualifiedName;

                    starter = EngineFactory
                        .Create(appContext.AppConfiguration)
                        .Start(appContext);

                    starter.ConfigureServices(services);

                })
                .ConfigureContainer<ContainerBuilder>((context, builder) =>
                {
                    starter.ConfigureContainer(builder);
                    starter.Dispose();

                    // Register mocked IGenericAttributeService, else generic attributes e.g. for customer will throw.
                    var genericAttributeServiceMock = new Mock<IGenericAttributeService>();
                    genericAttributeServiceMock
                        .Setup(x => x.GetAttributesForEntity(It.IsAny<string>(), It.IsAny<int>()))
                        .Returns<string, int>((name, id) =>
                        {
                            var query = new List<GenericAttribute> {
                                new GenericAttribute { Key = "", Value = "" },
                                new GenericAttribute { Key = SystemCustomerAttributeNames.DiscountCouponCode, Value = "CouponCode 1" }
                            }.AsQueryable();

                            return new GenericAttributeCollection(query, name, id, 0);
                        });

                    builder.RegisterInstance(genericAttributeServiceMock.Object).As<IGenericAttributeService>().SingleInstance();

                    // Register some dependencies which will be resolved by Autofac during obtaining PriceCalculators.
                    var productAttributeMaterializerMock = new Mock<IProductAttributeMaterializer>();
                    builder.RegisterInstance(productAttributeMaterializerMock.Object).As<IProductAttributeMaterializer>().SingleInstance();
                });

            return builder;
        }

        private void InitDbContext()
        {
            var dataSettings = new DataSettings
            {
                AppVersion = SmartstoreVersion.Version,
                ConnectionString = "Test",
                TenantName = "Default",
                TenantRoot = null, // TODO
                DbFactory = new TestDbFactory()
            };

            DataSettings.Instance = dataSettings;
            DataSettings.SetTestMode(true);
            
            var builder = new DbContextOptionsBuilder<SmartDbContext>()
                .UseDbFactory(factoryBuilder => 
                {
                    factoryBuilder
                        .AddModelAssemblies(new[]
                        { 
                            // Add all core models from Smartstore.Core assembly
                            typeof(SmartDbContext).Assembly
                        });
                });

            _dbContext = new SmartDbContext((DbContextOptions<SmartDbContext>)builder.Options);
            _dbContext.Database.EnsureCreated();
        }

        private void InitProviders()
        {
            _providerManager.RegisterProvider("FixedTaxRateTest", new FixedRateTestTaxProvider());
            _providerManager.RegisterProvider("FixedRateTestShippingRateComputationMethod", new FixedRateTestShippingRateComputationMethod());
            _providerManager.RegisterProvider("Payments.TestMethod", new TestPaymentMethod());
            _providerManager.RegisterProvider("CurrencyExchange.TestProvider", new TestExchangeRateProvider());
            //_providerManager.RegisterProvider(DatabaseMediaStorageProvider.SystemName, new TestDatabaseMediaStorageProvider());
        }

        private void InitModules()
        {
            var modules = new List<IModuleDescriptor>
            {
                new ModuleDescriptor { SystemName = "Smartstore.Tax" },
                new ModuleDescriptor { SystemName = "Smartstore.Shipping" },
                new ModuleDescriptor { SystemName = "Smartstore.Payment" },
            };

            //ModuleExplorer.ReferencedPlugins = plugins;
        }

        internal List<Lazy<IPriceCalculator, PriceCalculatorMetadata>> GetCalculators(
            IPriceCalculatorFactory priceCalculatorFactory,
            IDiscountService discountService,
            CatalogSettings catalogSettings)
        {
            var calculators = new List<Lazy<IPriceCalculator, PriceCalculatorMetadata>>();

            var productMetadata = new PriceCalculatorMetadata { ValidTargets = CalculatorTargets.Product, Order = CalculatorOrdering.Default + 10 };

            var attributePriceCalculator = new Lazy<IPriceCalculator, PriceCalculatorMetadata>(() =>
                new AttributePriceCalculator(priceCalculatorFactory, DbContext), productMetadata);
            calculators.Add(attributePriceCalculator);

            // TODO: (mh) (core) Mock ProductService
            var bundlePriceCalculator =
                new Lazy<IPriceCalculator, PriceCalculatorMetadata>(() =>
                    new BundlePriceCalculator(priceCalculatorFactory, null),
                    new() { ValidTargets = CalculatorTargets.Bundle, Order = CalculatorOrdering.Early });
            calculators.Add(bundlePriceCalculator);

            var discountPriceCalculator =
                new Lazy<IPriceCalculator, PriceCalculatorMetadata>(() =>
                    new DiscountPriceCalculator(DbContext, discountService, catalogSettings),
                    new() { ValidTargets = CalculatorTargets.All, Order = CalculatorOrdering.Late });
            calculators.Add(discountPriceCalculator);

            // TODO: (mh) (core) Mock CatalogSearchService & ProductService
            var groupedProductPriceCalculator =
                new Lazy<IPriceCalculator, PriceCalculatorMetadata>(() =>
                    new GroupedProductPriceCalculator(null, priceCalculatorFactory, null),
                    new() { ValidTargets = CalculatorTargets.GroupedProduct, Order = CalculatorOrdering.Early });
            calculators.Add(groupedProductPriceCalculator);

            var lowestPriceCalculator = new Lazy<IPriceCalculator, PriceCalculatorMetadata>(() =>
                new LowestPriceCalculator(), productMetadata);
            calculators.Add(lowestPriceCalculator);

            var offerPriceCalculator =
                new Lazy<IPriceCalculator, PriceCalculatorMetadata>(() =>
                    new OfferPriceCalculator(),
                    new() { ValidTargets = CalculatorTargets.Product | CalculatorTargets.Bundle, Order = CalculatorOrdering.Default });
            calculators.Add(offerPriceCalculator);

            // TODO: (mh) (core) Mock Materializer
            var preselectedPriceCalculator =
                new Lazy<IPriceCalculator, PriceCalculatorMetadata>(() =>
                    new PreselectedPriceCalculator(null),
                    new() { ValidTargets = CalculatorTargets.Product, Order = CalculatorOrdering.Early + 1 });
            calculators.Add(preselectedPriceCalculator);

            var tierPriceCalculator =
                new Lazy<IPriceCalculator, PriceCalculatorMetadata>(() =>
                    new TierPriceCalculator(),
                    new() { ValidTargets = CalculatorTargets.Product, Order = CalculatorOrdering.Default + 100 });
            calculators.Add(tierPriceCalculator);

            // Add custom calculator for additional charge.
            var customCalculator = new Lazy<IPriceCalculator, PriceCalculatorMetadata>(() =>
                new CustomAdditionalChargeCalculator(), new PriceCalculatorMetadata { ValidTargets = CalculatorTargets.Product, Order = CalculatorOrdering.Default });
            calculators.Add(customCalculator);

            return calculators;
        }
    }
}
