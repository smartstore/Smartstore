using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Utilities;

namespace Smartstore.Core.Tests.Catalog
{
    [TestFixture]
    public class ProductVariantAttributeCombinationTests : ServiceTestBase
    {
        const int _variantTestNumber = 1;
        const string _skuTemplate = "Product variant {0}";

        private readonly List<ProductVariantAttributeCombination> _testVariants = new()
        {
            CreateAttributeCombination(_variantTestNumber),
            CreateAttributeCombination(),
            CreateAttributeCombination(),
            CreateAttributeCombination()
        };

        [OneTimeSetUp]
        public new async Task SetUp()
        {
            DbContext.ProductVariantAttributeCombinations.AddRange(_testVariants);
            await DbContext.SaveChangesAsync();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            DbContext.ProductVariantAttributeCombinations.RemoveRange(_testVariants);
            await DbContext.SaveChangesAsync();
        }

        [Test]
        public void Can_create_variant_hashcode()
        {
            var pvac1 = CreateAttributeCombination(_variantTestNumber);
            Assert.IsTrue(pvac1.HashCode != 0);
            Assert.AreEqual(pvac1.HashCode, _testVariants[0].HashCode);

            // Selection with custom attributes like gift card data.
            var pvac2 = CreateAttributeCombination(_variantTestNumber, true);
            Assert.IsTrue(pvac2.HashCode != 0);
            Assert.AreEqual(pvac2.HashCode, _testVariants[0].HashCode);

            var sku = _skuTemplate.FormatInvariant(_variantTestNumber);
            var storedHashCode = DbContext.ProductVariantAttributeCombinations
                .Where(x => x.Sku == sku)
                .Select(x => x.HashCode)
                .FirstOrDefault();

            Assert.IsTrue(storedHashCode != 0);
            Assert.AreEqual(pvac1.HashCode, storedHashCode);
        }

        [Test]
        public async Task Can_find_variant_by_hashcode()
        {
            // TODO: (mg) include text attribute!
            var hashCode1 = CreateAttributeSelection(_variantTestNumber).GetHashCode();
            var hashCode2 = CreateAttributeSelection(-100).GetHashCode();
            Assert.IsTrue(hashCode1 != 0);
            Assert.IsTrue(hashCode2 != 0);

            var variant1 = await DbContext.ProductVariantAttributeCombinations.FirstOrDefaultAsync(x => x.ProductId == _variantTestNumber && x.HashCode == hashCode1);
            Assert.IsTrue(variant1 != null);
            Assert.IsTrue(variant1.Sku == _skuTemplate.FormatInvariant(_variantTestNumber));

            var variant2 = await DbContext.ProductVariantAttributeCombinations.FirstOrDefaultAsync(x => x.ProductId == _variantTestNumber && x.HashCode == hashCode2);
            Assert.IsTrue(variant2 == null);
        }

        private static ProductVariantAttributeSelection CreateAttributeSelection(int? anyNumber = null, bool includeGiftCard = false, bool includeTextAttribute = false)
        {
            var num = anyNumber ?? CommonHelper.GenerateRandomInteger(10, int.MaxValue - 100);
            var selection = new ProductVariantAttributeSelection(null);
            selection.AddAttribute(num + 1, new object[] { num + 10, num + 11, num + 12, num + 13 });
            selection.AddAttribute(num + 2, new object[] { num + 20 });

            if (includeTextAttribute)
            {
                selection.AddAttributeValue(num + 3, Path.GetRandomFileName().Replace(".", string.Empty));
            }

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

        private static ProductVariantAttributeCombination CreateAttributeCombination(int? anyNumber = null, bool includeGiftCard = false, bool includeTextAttribute = false)
        {
            var num = anyNumber ?? CommonHelper.GenerateRandomInteger(10, int.MaxValue - 100);
            var selection = CreateAttributeSelection(num, includeGiftCard, includeTextAttribute);

            return new()
            {
                Sku = _skuTemplate.FormatInvariant(num),
                RawAttributes = selection.AsJson(),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                ProductId = num
            };
        }
    }
}