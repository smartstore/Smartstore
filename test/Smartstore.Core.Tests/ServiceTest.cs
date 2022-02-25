using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Extensions.Logging;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Tests.Checkout.Payment;
using Smartstore.Core.Tests.Shipping;
using Smartstore.Core.Tests.Tax;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;

namespace Smartstore.Core.Tests
{
    [TestFixture]
    public abstract class ServiceTest
    {
        private MockProviderManager _providerManager = new MockProviderManager();
        private ModuleManager _moduleManager;
        private SmartDbContext _dbContext;
        private IEngine _engine;
        private WebApplicationBuilder _builder;

        [OneTimeSetUp]
        public void SetUp()
        {
            CommonHelper.IsHosted = false;
            CommonHelper.IsDevEnvironment = false;

            InitEngine();
            InitDbContext();

            _moduleManager = new ModuleManager(null, null, null, null, null, null, new Func<IModuleDescriptor, IModule>(x => null));

            InitModules();
            InitProviders();
        }

        private void InitEngine()
        {
            _builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = AppContext.BaseDirectory,
                WebRootPath = "TEST_ENGINE_NO_PATH"
            });

            // Add connections.json and usersettings.json to configuration manager
            var configuration = (IConfiguration)_builder.Configuration
                .AddJsonFile("Config/connections.json", optional: true, reloadOnChange: true)
                .AddJsonFile("Config/usersettings.json", optional: true, reloadOnChange: true);

            // Configure the host
            _builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            var startupLogger = new SerilogLoggerFactory(Log.Logger).CreateLogger("File");
            var appContext = new SmartApplicationContext(_builder.Environment, configuration, startupLogger);
            _engine = EngineFactory.Create(appContext.AppConfiguration);
            var engineStarter = _engine.Start(appContext);

            // Add services to the container.
            engineStarter.ConfigureServices(_builder.Services);

            // Overwrite GenericAttributeService
            var genericAttributeMockWrapper = new Mock<IGenericAttributeService>();
            var genericAttributeService = genericAttributeMockWrapper.Object;
            genericAttributeMockWrapper
                .Setup(x => x.GetAttributesForEntity("Customer", 1)).Returns(
                    new GenericAttributeCollection(
                        new List<GenericAttribute> {
                            new GenericAttribute() { Key = "", Value = "" },
                            new GenericAttribute() { Key = "", Value = "" }
                        }.AsQueryable()
                     , "Customer", 1, 0, null)
            );

            // Add services to the Autofac container.
            _builder.Host.ConfigureContainer<ContainerBuilder>(container =>
            {
                engineStarter.ConfigureContainer(container);

                // Register mocked GenericAttributeService else GenericAttributes e.g. for customer will throw.
                container.RegisterInstance(genericAttributeMockWrapper.Object).As<IGenericAttributeService>().SingleInstance();

                // Register some dependencies which will be resolved by Autofac during obtaining PriceCalculators.
                var productAttributeMaterializerWrapper = new Mock<IProductAttributeMaterializer>();
                container.RegisterInstance(productAttributeMaterializerWrapper.Object).As<IProductAttributeMaterializer>().SingleInstance();
            });

            // Build the application
            var app = _builder.Build();

            // At this stage we can access IServiceProvider.
            var providerContainer = appContext as IServiceProviderContainer;
            providerContainer.ApplicationServices = app.Services;

            // At this stage we can set the scoped service container.
            _engine.Scope = new ScopedServiceContainer(
                app.Services.GetRequiredService<ILifetimeScopeAccessor>(),
                app.Services.GetRequiredService<IHttpContextAccessor>(),
                app.Services.AsLifetimeScope());

            // Run application
            //app.Run();
        }

        private void InitDbContext()
        {
            var builder = new DbContextOptionsBuilder<SmartDbContext>()
                .UseInMemoryDatabase("Test")
                .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));

            var extension = new DbFactoryOptionsExtension(builder.Options);
            extension = extension.WithModelAssemblies(new[] { typeof(SmartDbContext).Assembly });
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

            _dbContext = new SmartDbContext(builder.Options);

            _dbContext.Database.EnsureCreated();

            //_builder.Host.ConfigureContainer<ContainerBuilder>(container =>
            //{
            //    container.RegisterInstance(_dbContext).As<SmartDbContext>().SingleInstance();
            //});
        }

        private void InitProviders()
        {
            _providerManager.RegisterProvider("FixedTaxRateTest", new FixedRateTestTaxProvider());
            _providerManager.RegisterProvider("FixedRateTestShippingRateComputationMethod", new FixedRateTestShippingRateComputationMethod());
            _providerManager.RegisterProvider("Payments.TestMethod", new TestPaymentMethod());
            //_providerManager.RegisterProvider("CurrencyExchange.TestProvider", new TestExchangeRateProvider());
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

        protected MockProviderManager ProviderManager => _providerManager;

        protected SmartDbContext DbContext => _dbContext;

        protected IEngine Engine => _engine;

        protected WebApplicationBuilder Builder => _builder;
    }
}
