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

            Assert.That(json, Is.Null);
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

            Assert.That(selection1, Is.EqualTo(selection2));
            Assert.That(selection1, Is.EqualTo(selection2));

            selection2.AddAttributeValue(123, 999);

            Assert.That(selection1, Is.Not.EqualTo(selection2));
            Assert.That(selection1, Is.Not.EqualTo(selection2));

            selection3.RemoveAttribute(123);

            Assert.That(selection1, Is.Not.EqualTo(selection3));
            Assert.That(selection1, Is.Not.EqualTo(selection3));

            selection2.ClearAttributes();
            selection3.ClearAttributes();

            Assert.That(selection2, Is.EqualTo(selection3));
            Assert.That(selection2, Is.EqualTo(selection3));

            var emptySelection1 = new ProductVariantAttributeSelection(null);
            var emptySelection2 = new ProductVariantAttributeSelection(null);
            ProductVariantAttributeSelection emptySelection3 = null;
            ProductVariantAttributeSelection emptySelection4 = null;

            Assert.That(emptySelection1, Is.EqualTo(emptySelection2));
            Assert.That(emptySelection1, Is.EqualTo(emptySelection2));
            Assert.That(emptySelection3, Is.EqualTo(emptySelection4));

            Assert.That(emptySelection1, Is.Not.EqualTo(emptySelection3));
            Assert.That(emptySelection1, Is.Not.EqualTo(emptySelection3));
        }

        private static readonly int[] expected = new[] { 11, 12, 13 };

        private static void ValidateSelection(ProductVariantAttributeSelection selection)
        {
            Assert.Multiple(() =>
            {
                Assert.That(selection.AttributesMap.Count(), Is.EqualTo(3));
                Assert.That(selection.AttributesMap.Count(x => x.Key == 987), Is.EqualTo(1));
            });

            var values = selection.GetAttributeValues(123)
                .Select(x => x.ToString())
                .Select(x => x.ToInt())
                .ToArray();
            values.ShouldSequenceEqual(expected);

            var textValue = selection.GetAttributeValues(375);

            Assert.Multiple(() =>
            {
                Assert.That(textValue.Count(), Is.EqualTo(1));
                Assert.That(textValue.First().ToString(), Is.EqualTo("Any text."));
            });

            var giftCard = selection.GetGiftCardInfo();

            Assert.That(giftCard, Is.Not.Null);
            Assert.That(giftCard.RecipientName == "John Doe" && giftCard.RecipientEmail == "jdow@web.com", Is.True);
            Assert.That(giftCard.Message, Is.Not.Empty);
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
