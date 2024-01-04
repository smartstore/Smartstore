using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Utilities;

namespace Smartstore.Core.Tests.Catalog
{
    [TestFixture]
    public class AttributeHashCodeTests : ServiceTestBase
    {
        const int TestNumber = 1;
        const int RandomNumberOffset = 1000;
        const string SkuTemplate = "Product variant {0}";

        private readonly List<ProductVariantAttributeCombination> _testAttributeCombinations = new()
        {
            CreateAttributeCombination(TestNumber),
            CreateAttributeCombination(),
            CreateAttributeCombination(),
            CreateAttributeCombination()
        };

        private ProductVariantAttributeCombination TestAttributeCombination
            => _testAttributeCombinations[0];

        [OneTimeSetUp]
        public new async Task SetUp()
        {
            DbContext.ProductVariantAttributeCombinations.AddRange(_testAttributeCombinations);
            await DbContext.SaveChangesAsync();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            DbContext.ProductVariantAttributeCombinations.RemoveRange(_testAttributeCombinations);
            await DbContext.SaveChangesAsync();
        }

        [TestCase()]
        [TestCase(true)]
        [TestCase(false, true)]
        [TestCase(false, false, true)]
        [TestCase(false, true, true)]
        public void Can_create_attribute_selection_hashcode(
            bool includeGiftCard = false,
            bool reverseAttributeOrder = false,
            bool reverseValueOrder = false)
        {
            var hashCode = CreateAttributeSelection(TestNumber, includeGiftCard, reverseAttributeOrder, reverseValueOrder).GetHashCode();

            Assert.Multiple(() =>
            {
                Assert.That(hashCode, Is.Not.EqualTo(0));
                Assert.That(TestAttributeCombination.HashCode, Is.EqualTo(hashCode));
            });
        }

        [TestCase()]
        [TestCase(true)]
        [TestCase(false, true)]
        [TestCase(false, false, true)]
        [TestCase(false, true, true)]
        public void Can_create_attribute_combination_hashcode(
            bool includeGiftCard = false,
            bool reverseAttributeOrder = false,
            bool reverseValueOrder = false)
        {
            var pvac = CreateAttributeCombination(TestNumber, includeGiftCard, reverseAttributeOrder, reverseValueOrder);

            Assert.Multiple(() =>
            {
                Assert.That(pvac.HashCode, Is.Not.EqualTo(0));
                Assert.That(TestAttributeCombination.HashCode, Is.EqualTo(pvac.HashCode));
            });

            var sku = SkuTemplate.FormatInvariant(TestNumber);
            var combi = DbContext.ProductVariantAttributeCombinations
                .Where(x => x.Sku == sku)
                .FirstOrDefault();
            var storedHashCode = combi.HashCode;

            Assert.That(storedHashCode, Is.Not.EqualTo(0));
            Assert.That(storedHashCode, Is.EqualTo(pvac.HashCode));
        }

        [Test]
        public void Can_update_attribute_combination_hashcode()
        {
            var pvac1 = CreateAttributeCombination(TestNumber);
            var pvac2 = CreateAttributeCombination();
            Assert.That(pvac1.HashCode, Is.Not.EqualTo(pvac2.HashCode));

            pvac1.RawAttributes = pvac2.RawAttributes;
            Assert.That(pvac1.HashCode, Is.EqualTo(pvac2.HashCode));
        }

        [Test]
        public void Can_create_format_independent_attribute_combination_hashcode()
        {
            var pvacXml = CreateAttributeCombination(TestNumber, asJson: false);
            var pvacJson = CreateAttributeCombination(TestNumber, asJson: true);

            Assert.That(pvacXml.RawAttributes, Is.Not.EqualTo(pvacJson.RawAttributes));
            Assert.That(pvacXml.HashCode, Is.EqualTo(pvacJson.HashCode));

            pvacXml.RawAttributes = pvacJson.RawAttributes;
            Assert.That(pvacXml.HashCode, Is.EqualTo(pvacJson.HashCode));
        }

        [TestCase()]
        [TestCase(true)]
        [TestCase(false, true)]
        [TestCase(false, false, true)]
        [TestCase(false, true, true)]
        public async Task Can_find_attribute_combination_by_hashcode(
            bool includeGiftCard = false,
            bool reverseAttributeOrder = false,
            bool reverseValueOrder = false)
        {
            var hashCode1 = CreateAttributeSelection(TestNumber, includeGiftCard, reverseAttributeOrder, reverseValueOrder).GetHashCode();
            var hashCode2 = CreateAttributeSelection(null, includeGiftCard, reverseAttributeOrder, reverseValueOrder).GetHashCode();
            Assert.That(hashCode1, Is.Not.EqualTo(0));
            Assert.That(hashCode2, Is.Not.EqualTo(0));

            var variant1 = await DbContext.ProductVariantAttributeCombinations.ApplyHashCodeFilter(TestNumber, hashCode1);
            Assert.That(variant1, Is.Not.Null);
            Assert.That(variant1.Sku, Is.EqualTo(SkuTemplate.FormatInvariant(TestNumber)));

            var variant2 = await DbContext.ProductVariantAttributeCombinations.ApplyHashCodeFilter(TestNumber, hashCode2);
            Assert.That(variant2, Is.Null);
        }

        private static ProductVariantAttributeSelection CreateAttributeSelection(
            int? anyNumber = null,
            bool includeGiftCard = false,
            bool reverseAttributeOrder = false,
            bool reverseValueOrder = false)
        {
            var num = anyNumber ?? CommonHelper.GenerateRandomInteger(RandomNumberOffset, int.MaxValue - 100);
            var selection = new ProductVariantAttributeSelection(null);

            IEnumerable<object> values1 = new object[] { num + 10, num + 11, num + 12, num + 13 };
            IEnumerable<object> values2 = new object[] { num + 20 };

            if (reverseValueOrder)
            {
                values1 = values1.Reverse();
            }

            if (reverseAttributeOrder)
            {
                selection.AddAttribute(num + 2, values2);
                selection.AddAttribute(num + 1, values1);
            }
            else
            {
                selection.AddAttribute(num + 1, values1);
                selection.AddAttribute(num + 2, values2);
            }

            //if (includeTextAttribute)
            //{
            //    selection.AddAttributeValue(num + 3, Path.GetRandomFileName().Replace(".", string.Empty));
            //}

            if (includeGiftCard)
            {
                selection.AddGiftCardInfo(new GiftCardInfo
                {
                    RecipientName = "John Doe",
                    RecipientEmail = "jdow@web.com",
                    SenderName = "me",
                    SenderEmail = "me@web.com",
                    Message = "Hello world!"
                });
            }

            return selection;
        }

        private static ProductVariantAttributeCombination CreateAttributeCombination(
            int? anyNumber = null, 
            bool includeGiftCard = false,
            bool reverseAttributeOrder = false,
            bool reverseValueOrder = false,
            bool asJson = true)
        {
            var num = anyNumber ?? CommonHelper.GenerateRandomInteger(RandomNumberOffset, int.MaxValue - 100);
            var selection = CreateAttributeSelection(num, includeGiftCard, reverseAttributeOrder, reverseValueOrder);

            return new()
            {
                Sku = SkuTemplate.FormatInvariant(num),
                RawAttributes = asJson ? selection.AsJson() : selection.AsXml(),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                ProductId = num
            };
        }
    }
}