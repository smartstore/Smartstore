using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Tax
{
    [TestFixture]
    public class TaxServiceTests : ServiceTestBase
    {
        IWorkContext _workContext;
        IRoundingHelper _roundingHelper;
        ITaxService _taxService;
        CurrencySettings _currencySettings;
        Currency _currency;

        [OneTimeSetUp]
        public new void SetUp()
        {
            _currencySettings = new CurrencySettings();
            _currency = new Currency { Id = 1 };

            var workContextMock = new Mock<IWorkContext>();
            _workContext = workContextMock.Object;
            workContextMock.Setup(x => x.WorkingCurrency).Returns(_currency);

            _roundingHelper = new RoundingHelper(_workContext, _currencySettings);

            _taxService = new TaxService(
                DbContext,
                null,
                ProviderManager,
                null,
                _roundingHelper,
                null,
                new TaxSettings { DefaultTaxAddressId = 10, EuVatUseWebService = true });
        }

        [Test]
        public void Can_load_taxProviders()
        {
            var providers = _taxService.LoadAllTaxProviders();
            providers.ShouldNotBeNull();
            providers.Any().ShouldBeTrue();
        }

        [Test]
        public void Can_load_taxProvider_by_systemKeyword()
        {
            var provider = _taxService.LoadTaxProviderBySystemName("FixedTaxRateTest");
            provider.ShouldNotBeNull();
        }

        [Test]
        public void Can_load_active_taxProvider()
        {
            var provider = _taxService.LoadActiveTaxProvider();
            provider.ShouldNotBeNull();
        }

        [Test]
        public async Task Can_check_taxExempt_product()
        {
            var product = new Product
            {
                IsTaxExempt = true
            };
            (await _taxService.IsTaxExemptAsync(product, null)).ShouldEqual(true);

            product.IsTaxExempt = false;
            (await _taxService.IsTaxExemptAsync(product, null)).ShouldEqual(false);
        }

        [Test]
        public async Task Can_check_taxExempt_customer()
        {
            var customer = new Customer
            {
                IsTaxExempt = true
            };
            (await _taxService.IsTaxExemptAsync(null, customer)).ShouldEqual(true);

            customer.IsTaxExempt = false;
            (await _taxService.IsTaxExemptAsync(null, customer)).ShouldEqual(false);
        }

        [Test]
        public async Task Can_check_taxExempt_customer_in_taxExemptCustomerRole()
        {
            var customer = new Customer();
            customer.IsTaxExempt = false;
            (await _taxService.IsTaxExemptAsync(null, customer)).ShouldEqual(false);

            var customerRole = new CustomerRole
            {
                TaxExempt = true,
                Active = true
            };

            customer.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = customer.Id,
                CustomerRole = customerRole
            });
            (await _taxService.IsTaxExemptAsync(null, customer)).ShouldEqual(true);

            customerRole.TaxExempt = false;
            (await _taxService.IsTaxExemptAsync(null, customer)).ShouldEqual(false);

            // If role is not active, weshould ignore 'TaxExempt' property.
            customerRole.Active = false;
            (await _taxService.IsTaxExemptAsync(null, customer)).ShouldEqual(false);
        }

        // TODO: (mh) (core) Implement test for Can_get_productPrice_priceIncludesTax_includingTax in PriceCalculation tests.

        [TestCase("DE814160246", VatNumberStatus.Valid)]
        [TestCase("DE000000000", VatNumberStatus.Invalid)]
        public async Task Can_check_VAT_number(string vatNumber, VatNumberStatus status)
        {
            var result = await _taxService.GetVatNumberStatusAsync(vatNumber);

            if (status == VatNumberStatus.Invalid)
            {
                result.Exception.ShouldBeNull();
                result.Status.ShouldEqual(status);
            }
            else if (result.Exception == null)
            {
                result.Status.ShouldEqual(status);
            }
            else
            {
                Assert.Warn($"{nameof(ITaxService.GetVatNumberStatusAsync)} threw an exception. Is the VAT check service perhaps temporarily out of service? {result.Exception.Message}");
            }
        }
    }
}
