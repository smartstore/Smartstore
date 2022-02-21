using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Checkout.Payment
{
    [TestFixture]
    public class PaymentServiceTests : ServiceTest
    {
        IPaymentService _paymentService;
        PaymentSettings _paymentSettings;

        //IRepository<PaymentMethod> _paymentMethodRepository;
        //IRepository<StoreMapping> _storeMappingRepository;
        //IStoreMappingService _storeMappingService;
        //ICartRuleProvider _cartRuleProvider;
        //ICommonServices _services;
        //ITypeFinder _typeFinder;

        [SetUp]
        public new void SetUp()
        {
            _paymentSettings = new PaymentSettings
            {
                ActivePaymentMethodSystemNames = new List<string>()
            };
            
            _paymentSettings.ActivePaymentMethodSystemNames.Add("Payments.TestMethod");

            var paymentMethods = new List<PaymentMethod> { new PaymentMethod { PaymentMethodSystemName = "Payments.TestMethod" } };

            _paymentService = new PaymentService(null, null, _paymentSettings, null, ProviderManager, null, null);

            //_storeMappingRepository = MockRepository.GenerateMock<IRepository<StoreMapping>>();
            //_storeMappingService = MockRepository.GenerateMock<IStoreMappingService>();
            //_cartRuleProvider = MockRepository.GenerateMock<ICartRuleProvider>();

            //_services = MockRepository.GenerateMock<ICommonServices>();
            //_services.Expect(x => x.RequestCache).Return(NullRequestCache.Instance);

            //_paymentMethodRepository = MockRepository.GenerateMock<IRepository<PaymentMethod>>();
            //_paymentMethodRepository.Expect(x => x.TableUntracked).Return(paymentMethods.AsQueryable());

            //_typeFinder = MockRepository.GenerateMock<ITypeFinder>();
            //_typeFinder.Expect(x => x.FindClassesOfType((Type)null, null, true)).IgnoreArguments().Return(Enumerable.Empty<Type>()).Repeat.Any();

            //var localizationService = MockRepository.GenerateMock<ILocalizationService>();
            //localizationService.Expect(ls => ls.GetResource(null)).IgnoreArguments().Return("NotSupported").Repeat.Any();

            //_paymentService = new PaymentService(_paymentMethodRepository, _storeMappingRepository, _storeMappingService, _paymentSettings, _cartRuleProvider,
            //    this.ProviderManager, _services, _typeFinder);
        }

        [Test]
        public async Task Can_load_paymentMethod_by_systemKeywordAsync()
        {
            var srcm = await _paymentService.LoadPaymentMethodBySystemNameAsync("Payments.TestMethod");
            srcm.ShouldNotBeNull();
        }

        [Test]
        public async Task Can_load_active_paymentMethodsAsync()
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
