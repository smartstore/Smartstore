using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Tests.Checkout.Payment;
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
        private ModuleManager _moduleManager;
        private SmartDbContext _dbContext;
        private IEngine _engine;
        private ILifetimeScope _lifetimeScope;
        private IDisposable _lifetimeToken;
        private WebApplicationBuilder _builder;

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

            // Initialize memory DbContext
            InitDbContext();

            InitProviders();

            _lifetimeToken = scopeAccessor.BeginContextAwareScope(out var _lifetimeScope);
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
                                new GenericAttribute { Key = "", Value = "" }
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
    }
}
