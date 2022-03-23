using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Engine;
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

        [OneTimeSetUp]
        public new void SetUp()
        {
            _paymentSettings = new PaymentSettings
            {
                ActivePaymentMethodSystemNames = new List<string>()
            };
            
            _paymentSettings.ActivePaymentMethodSystemNames.Add("Payments.TestMethod1");

            var typeScannerMock = new Mock<ITypeScanner>();
            _typeScanner = typeScannerMock.Object;

            var requestCacheMock = new Mock<IRequestCache>();
            _requestCache = requestCacheMock.Object;

            _paymentService = new PaymentService(null, null, _paymentSettings, null, ProviderManager, _requestCache, _typeScanner);
        }

        [Test]
        public async Task Can_load_paymentMethod_by_systemKeyword()
        {
            var srcm = await _paymentService.LoadPaymentMethodBySystemNameAsync("Payments.TestMethod1");
            srcm.ShouldNotBeNull();
        }

        [Test]
        public async Task Can_load_active_paymentMethods()
        {
            var srcm = await _paymentService.LoadActivePaymentMethodsAsync();
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