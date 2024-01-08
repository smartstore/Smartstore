using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Checkout.Payment
{
    [TestFixture]
    public class PaymentServiceTests : ServiceTestBase
    {
        IPaymentService _paymentService;
        PaymentSettings _paymentSettings;
        ITypeScanner _typeScanner;
        IRequestCache _requestCache;
        IStoreContext _storeContext;

        [OneTimeSetUp]
        public new void SetUp()
        {
            _paymentSettings = new PaymentSettings
            {
                ActivePaymentMethodSystemNames = []
            };

            _paymentSettings.ActivePaymentMethodSystemNames.Add("Payments.TestMethod1");

            var storeContextMock = new Mock<IStoreContext>();
            _storeContext = storeContextMock.Object;

            var typeScannerMock = new Mock<ITypeScanner>();
            _typeScanner = typeScannerMock.Object;

            var requestCacheMock = new Mock<IRequestCache>();
            _requestCache = requestCacheMock.Object;

            var ruleProviderFactoryMock = new Mock<IRuleProviderFactory>();
            ruleProviderFactoryMock.Setup(x => x.GetProvider(RuleScope.Cart, null)).Returns(new Mock<ICartRuleProvider>().Object);

            _paymentService = new PaymentService(
                DbContext, 
                _storeContext, 
                null, 
                _paymentSettings,
                ruleProviderFactoryMock.Object,
                ProviderManager,
                NullCache.Instance,
                _requestCache, 
                _typeScanner, 
                new NullModuleContraint());
        }

        [Test]
        public async Task Can_load_paymentMethod_by_systemKeyword()
        {
            var srcm = await _paymentService.LoadPaymentProviderBySystemNameAsync("Payments.TestMethod1");
            srcm.ShouldNotBeNull();
        }

        [Test]
        public async Task Can_load_active_paymentMethods()
        {
            var srcm = await _paymentService.LoadActivePaymentProvidersAsync();
            srcm.ShouldNotBeNull();
            srcm.Any().ShouldBeTrue();
        }

        [Test]
        public void Can_get_masked_credit_card_number()
        {
            _paymentService.GetMaskedCreditCardNumber("").ShouldEqual("");
            _paymentService.GetMaskedCreditCardNumber("123").ShouldEqual("123");
            _paymentService.GetMaskedCreditCardNumber("1234567890123456").ShouldEqual("************3456");
        }
    }
}