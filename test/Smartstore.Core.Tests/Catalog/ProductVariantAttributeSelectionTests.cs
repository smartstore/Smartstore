using System.Linq;
using NUnit.Framework;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Catalog
{
    [TestFixture]
    public class ProductVariantAttributeSelectionTests
    {
        private ProductVariantAttributeSelection2 _selectionWithGiftCard;

        [SetUp]
        public void SetUp()
        {
            _selectionWithGiftCard = new ProductVariantAttributeSelection2(null);

            _selectionWithGiftCard.AddAttribute(123, new object[] { 11, 12, 13 });
            _selectionWithGiftCard.AddAttribute(987, new object[] { 36 });

            _selectionWithGiftCard.AddGiftCardInfo(new GiftCardInfo
            {
                RecipientName = "John Doe",
                RecipientEmail = "jdow@web.com",
                SenderName = "me",
                SenderEmail = "me@web.com",
                Message = "Hello world!"
            });
        }

        [Test]
        public void CanSerializeAttributeSelectionToJson()
        {
            var json = _selectionWithGiftCard.AsJson();
            var selection = new ProductVariantAttributeSelection2(json);

            ValidateSelection(selection);
        }

        [Test]
        public void CanSerializeAttributeSelectionToXml()
        {
            var xml = _selectionWithGiftCard.AsXml();
            var selection = new ProductVariantAttributeSelection2(xml);

            ValidateSelection(selection);
        }

        private void ValidateSelection(ProductVariantAttributeSelection2 selection)
        {
            Assert.AreEqual(selection.AttributesMap.Count(), 2);
            Assert.IsNotNull(selection.AttributesMap.FirstOrDefault(x => x.Key == 987));

            var values = selection.GetAttributeValues(123)
                .Select(x => x.ToString())
                .Select(x => x.ToInt())
                .ToArray();
            values.ShouldSequenceEqual(new[] { 11, 12, 13 });

            var giftCard = selection.GiftCardInfo;

            Assert.IsNotNull(giftCard);
            Assert.IsTrue(giftCard.RecipientName == "John Doe" && giftCard.RecipientEmail == "jdow@web.com");
            Assert.IsNotEmpty(giftCard.Message);
        }
    }
}
