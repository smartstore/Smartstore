using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Tax
{
    [TestFixture]
    public class TaxServiceTests : TestsBase
    {
        ITaxService _taxService;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _taxService = new TaxService(
                null,
                null,
                null,
                new TaxSettings { DefaultTaxAddressId = 10, EuVatUseWebService = true },
                null);
        }

        [Test]
        public async Task Can_do_VAT_check()
        {
            // Check VAT of DB Vertrieb GmbH (Deutsche Bahn).
            var vatNumberStatus1 = await _taxService.GetVatNumberStatusAsync("DE814160246");
            if (vatNumberStatus1.Exception == null)
            {
                vatNumberStatus1.Status.ShouldEqual(VatNumberStatus.Valid);
            }

            var vatNumberStatus2 = await _taxService.GetVatNumberStatusAsync("DE000000000");
            vatNumberStatus2.Status.ShouldEqual(VatNumberStatus.Invalid);
            vatNumberStatus2.Exception.ShouldBeNull();
        }
    }
}
