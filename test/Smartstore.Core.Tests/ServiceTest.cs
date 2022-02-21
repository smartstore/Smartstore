using System;
using System.Collections.Generic;
using NUnit.Framework;
using Smartstore.Core.Tests.Checkout.Payment;
using Smartstore.Core.Tests.Shipping;
using Smartstore.Core.Tests.Tax;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Tests
{
    [TestFixture]
    public abstract class ServiceTest
    {
        private MockProviderManager _providerManager = new MockProviderManager();
        ModuleManager _moduleManager;

        [SetUp]
        public void SetUp()
        {
            CommonHelper.IsHosted = false;
            CommonHelper.IsDevEnvironment = false;

            _moduleManager = new ModuleManager(null, null, null, null, null, null, new Func<IModuleDescriptor, IModule>(x => null));

            InitModules();
            InitProviders();
        }

        private void InitProviders()
        {
            _providerManager.RegisterProvider("FixedTaxRateTest", new FixedRateTestTaxProvider());
            _providerManager.RegisterProvider("FixedRateTestShippingRateComputationMethod", new FixedRateTestShippingRateComputationMethod());
            //_providerManager.RegisterProvider("CurrencyExchange.TestProvider", new TestExchangeRateProvider());
            _providerManager.RegisterProvider("Payments.TestMethod", new TestPaymentMethod());
            //_providerManager.RegisterProvider(DatabaseMediaStorageProvider.SystemName, new TestDatabaseMediaStorageProvider());
        }

        private void InitModules()
        {
            var modules = new List<IModuleDescriptor>();
            var fileSystem = new LocalFileSystem(AppDomain.CurrentDomain.BaseDirectory + "FakeModules\\");

            var taxRateTestModule = ModuleDescriptor.Create(fileSystem.GetDirectory("Smartstore.Tax"), fileSystem);
            _moduleManager.CreateInstance(taxRateTestModule);
            modules.Add(taxRateTestModule);

            var shippingRateTestModule = ModuleDescriptor.Create(fileSystem.GetDirectory("Smartstore.Shipping"), fileSystem);
            _moduleManager.CreateInstance(shippingRateTestModule);
            modules.Add(shippingRateTestModule);

            var paymentTestModule = ModuleDescriptor.Create(fileSystem.GetDirectory("Smartstore.Payment"), fileSystem);
            _moduleManager.CreateInstance(paymentTestModule);
            modules.Add(paymentTestModule);

            //modules.Add(new PluginDescriptor(typeof(TestExchangeRateProvider).Assembly, null, typeof(TestExchangeRateProvider))
            //{
            //    SystemName = "CurrencyExchange.TestProvider",
            //    FriendlyName = "Test exchange rate provider",
            //    Installed = true,
            //});

            //PluginManager.ReferencedPlugins = modules;
        }

        protected MockProviderManager ProviderManager => _providerManager;
    }
}
