using System.ComponentModel;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Smartstore.Core.Checkout.GiftCards;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Represents a product variant attribute selection.
    /// </summary>
    /// <remarks>This class can parse strings of XML or JSON format.</remarks>
    public class ProductVariantAttributeSelection : AttributeSelection
    {
        private const string GIFTCARD_ATTRIBUTE_NAME = "GiftCardInfo";

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
            var value = GetCustomAttributeValues(GIFTCARD_ATTRIBUTE_NAME)?.FirstOrDefault();

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
            AddCustomAttributeValue(GIFTCARD_ATTRIBUTE_NAME, giftCard);
        }

        protected override object ToCustomAttributeValue(string attributeName, object value)
        {
            if (value != null && attributeName.EqualsNoCase(GIFTCARD_ATTRIBUTE_NAME))
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
                else if (value is JObject jObj)
                {
                    dynamic o = jObj;

                    return new GiftCardInfo
                    {
                        RecipientEmail = o.RecipientEmail,
                        RecipientName = o.RecipientName,
                        SenderName = o.SenderName,
                        SenderEmail = o.SenderEmail,
                        Message = o.Message
                    };
                }
            }

            return null;
        }

        protected override XElement ToCustomAttributeElement(object value)
        {
            if (value is GiftCardInfo giftCardInfo)
            {
                var el = new XElement(GIFTCARD_ATTRIBUTE_NAME);

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