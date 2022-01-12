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
        [Test]
        public void CanSerializeAttributeSelectionToJson()
        {
            var source = CreateSelection();
            var json = source.AsJson();
            var selection = new ProductVariantAttributeSelection(json);

            ValidateSelection(selection);
        }

        [Test]
        public void CanSerializeEmptyAttributeSelectionToJson()
        {
            var source = new ProductVariantAttributeSelection(string.Empty);
            var json = source.AsJson();

            Assert.IsNull(json);
        }

        [Test]
        public void CanSerializeAttributeSelectionToXml()
        {
            var source = CreateSelection();
            var xml = source.AsXml();
            var selection = new ProductVariantAttributeSelection(xml);

            ValidateSelection(selection);
        }

        [Test]
        public void AttributeSelectionEqualTo()
        {
            var selection1 = CreateSelection();
            var selection2 = CreateSelection();
            var selection3 = CreateSelection();

            Assert.IsTrue(selection1 == selection2);
            Assert.IsTrue(selection1.Equals(selection2));

            selection2.AddAttributeValue(123, 999);

            Assert.IsFalse(selection1 == selection2);
            Assert.IsFalse(selection1.Equals(selection2));

            selection3.RemoveAttribute(123);

            Assert.IsFalse(selection1 == selection3);
            Assert.IsFalse(selection1.Equals(selection3));

            selection2.ClearAttributes();
            selection3.ClearAttributes();

            Assert.IsTrue(selection2 == selection3);
            Assert.IsTrue(selection2.Equals(selection3));

            var emptySelection1 = new ProductVariantAttributeSelection(null);
            var emptySelection2 = new ProductVariantAttributeSelection(null);
            ProductVariantAttributeSelection emptySelection3 = null;
            ProductVariantAttributeSelection emptySelection4 = null;

            Assert.IsTrue(emptySelection1 == emptySelection2);
            Assert.IsTrue(emptySelection1.Equals(emptySelection2));
            Assert.IsTrue(emptySelection3 == emptySelection4);

            Assert.IsFalse(emptySelection1 == emptySelection3);
            Assert.IsFalse(emptySelection1.Equals(emptySelection3));
        }

        private static void ValidateSelection(ProductVariantAttributeSelection selection)
        {
            Assert.AreEqual(selection.AttributesMap.Count(), 3);
            Assert.IsNotNull(selection.AttributesMap.FirstOrDefault(x => x.Key == 987));

            var values = selection.GetAttributeValues(123)
                .Select(x => x.ToString())
                .Select(x => x.ToInt())
                .ToArray();
            values.ShouldSequenceEqual(new[] { 11, 12, 13 });

            var textValue = selection.GetAttributeValues(375);
            Assert.AreEqual(textValue.Count(), 1);
            Assert.AreEqual(textValue.First().ToString(), "Any text.");

            var giftCard = selection.GetGiftCardInfo();

            Assert.IsNotNull(giftCard);
            Assert.IsTrue(giftCard.RecipientName == "John Doe" && giftCard.RecipientEmail == "jdow@web.com");
            Assert.IsNotEmpty(giftCard.Message);
        }

        private static ProductVariantAttributeSelection CreateSelection()
        {
            var selection = new ProductVariantAttributeSelection(null);

            selection.AddAttribute(123, new object[] { 11, 12, 13 });
            selection.AddAttribute(987, new object[] { 36 });
            selection.AddAttributeValue(375, "Any text.");

            selection.AddGiftCardInfo(new GiftCardInfo
            {
                RecipientName = "John Doe",
                RecipientEmail = "jdow@web.com",
                SenderName = "me",
                SenderEmail = "me@web.com",
                Message = "Hello world!"
            });

            return selection;
        }
    }
}
