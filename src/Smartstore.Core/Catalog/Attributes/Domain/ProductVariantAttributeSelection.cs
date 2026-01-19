using System.ComponentModel;
using System.Text.Json;
using System.Xml.Linq;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Json;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Represents a product variant attribute selection.
    /// </summary>
    /// <remarks>This class can parse strings of XML or JSON format.</remarks>
    public class ProductVariantAttributeSelection : AttributeSelection
    {
        const string GiftCardAttributeName = "GiftCardInfo";

        /// <summary>
        /// Creates product variant attribute selection from string. 
        /// Use <see cref="AttributeSelection.AttributesMap"/> to access parsed attributes afterwards.
        /// </summary>
        /// <remarks>Automatically differentiates between XML and JSON.</remarks>
        /// <param name="rawAttributes">XML or JSON attributes string.</param>  
        public ProductVariantAttributeSelection(string rawAttributes)
            : base(rawAttributes, "ProductVariantAttribute")
        {
        }

        /// <summary>
        /// Gets the gift card information.
        /// </summary>
        public GiftCardInfo GetGiftCardInfo()
        {
            var value = GetCustomAttributeValues(GiftCardAttributeName)?.FirstOrDefault();

            return value != null && value is GiftCardInfo info
                ? info
                : null;
        }

        /// <summary>
        /// Adds gift card infomation to be taken into account when serializing attributes.
        /// </summary>
        /// <param name="giftCard">Gift card information.</param>
        public void AddGiftCardInfo(GiftCardInfo giftCard)
        {
            AddCustomAttributeValue(GiftCardAttributeName, giftCard);
        }

        protected override object ToCustomAttributeValue(string attributeName, object value)
        {
            if (value != null && attributeName.EqualsNoCase(GiftCardAttributeName))
            {
                if (value is XElement xElement)
                {
                    var giftCardInfo = new GiftCardInfo();

                    foreach (var el in xElement.Elements())
                    {
                        switch (el.Name.LocalName)
                        {
                            case nameof(giftCardInfo.RecipientEmail):
                                giftCardInfo.RecipientEmail = el.Value;
                                break;
                            case nameof(giftCardInfo.RecipientName):
                                giftCardInfo.RecipientName = el.Value;
                                break;
                            case nameof(giftCardInfo.SenderName):
                                giftCardInfo.SenderName = el.Value;
                                break;
                            case nameof(giftCardInfo.SenderEmail):
                                giftCardInfo.SenderEmail = el.Value;
                                break;
                            case nameof(giftCardInfo.Message):
                                giftCardInfo.Message = el.Value;
                                break;
                            default:
                                throw new InvalidEnumArgumentException(el.Name.LocalName);
                        }
                    }

                    return giftCardInfo;
                }
                else if (value is JsonElement el)
                {
                    return JsonSerializer.Deserialize<GiftCardInfo>(el.GetRawText(), SmartJsonOptions.CamelCased);
                }
                else if (value is GiftCardInfo gci)
                {
                    return gci;
                }
            }

            return null;
        }

        protected override XElement ToCustomAttributeElement(object value)
        {
            if (value is GiftCardInfo giftCardInfo)
            {
                var el = new XElement(GiftCardAttributeName);

                el.Add(new XElement(nameof(giftCardInfo.RecipientName), giftCardInfo.RecipientName));
                el.Add(new XElement(nameof(giftCardInfo.RecipientEmail), giftCardInfo.RecipientEmail));
                el.Add(new XElement(nameof(giftCardInfo.SenderName), giftCardInfo.SenderName));
                el.Add(new XElement(nameof(giftCardInfo.SenderEmail), giftCardInfo.SenderEmail));
                el.Add(new XElement(nameof(giftCardInfo.Message), giftCardInfo.Message));

                return el;
            }

            return null;
        }
    }
}