using System;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Smartstore.Collections;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Represents a product variant attribute selection.
    /// </summary>
    /// <remarks>
    /// This class can parse strings with XML or JSON format to <see cref="Multimap{int, object}"/> and vice versa.
    /// </remarks>
    public class ProductVariantAttributeSelection : AttributeSelection
    {
        /// <summary>
        /// Creates product variant attribute selection from string as <see cref="Multimap{int, object}"/>. 
        /// Use <see cref="AttributeSelection.AttributesMap"/> to access parsed attributes afterwards.
        /// </summary>
        /// <remarks>
        /// Automatically differentiates between XML and JSON.
        /// </remarks>
        /// <param name="rawAttributes">XML or JSON attributes string.</param>  
        public ProductVariantAttributeSelection(string rawAttributes)
            : base(rawAttributes, "ProductVariantAttribute")
        {
        }

        /// <summary>
        /// Gets or sets gift card info
        /// </summary>
        public GiftCardInfo GiftCardInfo { get; set; }

        protected override void MapUnknownElement(XElement element, Multimap<int, object> map)
        {
            if (element.Name.LocalName == "GiftCardInfo")
            {
                try
                {
                    GiftCardInfo = new GiftCardInfo();

                    foreach (var el in element.Elements())
                    {
                        switch (el.Name.LocalName)
                        {
                            case nameof(GiftCardInfo.RecipientEmail):
                                GiftCardInfo.RecipientEmail = el.Value;
                                break;
                            case nameof(GiftCardInfo.RecipientName):
                                GiftCardInfo.RecipientName = el.Value;
                                break;
                            case nameof(GiftCardInfo.SenderName):
                                GiftCardInfo.SenderName = el.Value;
                                break;
                            case nameof(GiftCardInfo.SenderEmail):
                                GiftCardInfo.SenderEmail = el.Value;
                                break;
                            case nameof(GiftCardInfo.Message):
                                GiftCardInfo.Message = el.Value;
                                break;
                            default:
                                throw new InvalidEnumArgumentException(el.Name.LocalName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new XmlException("Error while trying to parse from additional XML: " + nameof(ProductVariantAttributeSelection), ex);
                }
            }
        }

        protected override void OnSerialize(XElement root)
        {
            if (GiftCardInfo != null)
            {
                var el = new XElement("GiftCardInfo");
                el.Add(new XElement(nameof(GiftCardInfo.RecipientName), GiftCardInfo.RecipientName));
                el.Add(new XElement(nameof(GiftCardInfo.RecipientEmail), GiftCardInfo.RecipientEmail));
                el.Add(new XElement(nameof(GiftCardInfo.SenderName), GiftCardInfo.SenderName));
                el.Add(new XElement(nameof(GiftCardInfo.SenderEmail), GiftCardInfo.SenderEmail));
                el.Add(new XElement(nameof(GiftCardInfo.Message), GiftCardInfo.Message));

                root.Add(el);
            }
        }
    }


    public class ProductVariantAttributeSelection2 : AttributeSelection2
    {
        private const string GIFTCARD_ATTRIBUTE_NAME = "GiftCardInfo";
        private GiftCardInfo _giftCardInfo;

        /// <summary>
        /// Creates product variant attribute selection from string. 
        /// Use <see cref="AttributeSelection.AttributesMap"/> to access parsed attributes afterwards.
        /// </summary>
        /// <remarks>
        /// Automatically differentiates between XML and JSON.
        /// </remarks>
        /// <param name="rawAttributes">XML or JSON attributes string.</param>  
        public ProductVariantAttributeSelection2(string rawAttributes)
            : base(rawAttributes, "ProductVariantAttribute")
        {
        }

        public GiftCardInfo GiftCardInfo
        {
            get
            {
                if (_giftCardInfo == null)
                {
                    var value = GetCustomAttributeValues(GIFTCARD_ATTRIBUTE_NAME).FirstOrDefault();

                    _giftCardInfo = value != null && value is GiftCardInfo info
                        ? info
                        : new GiftCardInfo();
                }

                return _giftCardInfo;
            }
        }

        public void AddGiftCardInfo(GiftCardInfo giftCard)
        {
            AddCustomAttribute(GIFTCARD_ATTRIBUTE_NAME, giftCard);
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
                            case nameof(GiftCardInfo.RecipientEmail):
                                giftCardInfo.RecipientEmail = el.Value;
                                break;
                            case nameof(GiftCardInfo.RecipientName):
                                giftCardInfo.RecipientName = el.Value;
                                break;
                            case nameof(GiftCardInfo.SenderName):
                                giftCardInfo.SenderName = el.Value;
                                break;
                            case nameof(GiftCardInfo.SenderEmail):
                                giftCardInfo.SenderEmail = el.Value;
                                break;
                            case nameof(GiftCardInfo.Message):
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

                el.Add(new XElement(nameof(GiftCardInfo.RecipientName), giftCardInfo.RecipientName));
                el.Add(new XElement(nameof(GiftCardInfo.RecipientEmail), giftCardInfo.RecipientEmail));
                el.Add(new XElement(nameof(GiftCardInfo.SenderName), giftCardInfo.SenderName));
                el.Add(new XElement(nameof(GiftCardInfo.SenderEmail), giftCardInfo.SenderEmail));
                el.Add(new XElement(nameof(GiftCardInfo.Message), giftCardInfo.Message));

                return el;
            }

            return null;
        }
    }
}