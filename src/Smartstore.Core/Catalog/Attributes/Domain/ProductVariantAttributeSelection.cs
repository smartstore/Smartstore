using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;
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

        public GiftCardInfo GiftCardInfo { get; private set; }

        protected override void MapElement(XElement element, Multimap<int, object> map)
        {
            if (element.Name.LocalName == "GiftCardInfo")
            {
                try
                {
                    var giftCardInfo = new GiftCardInfo();
                    foreach (var el in element.Elements())
                    {
                        switch (el.Name.LocalName)
                        {
                            case nameof(GiftCardInfo.RecipientEmail):
                                {
                                    giftCardInfo.RecipientEmail = el.Value;
                                    break;
                                }
                            case nameof(GiftCardInfo.RecipientName):
                                {
                                    giftCardInfo.RecipientName = el.Value;
                                    break;
                                }
                            case nameof(GiftCardInfo.SenderName):
                                {
                                    giftCardInfo.SenderName = el.Value;
                                    break;
                                }
                            case nameof(GiftCardInfo.SenderEmail):
                                {
                                    giftCardInfo.SenderEmail = el.Value;
                                    break;
                                }
                            case nameof(GiftCardInfo.Message):
                                {
                                    giftCardInfo.Message = el.Value;
                                    break;
                                }

                            default:
                                throw new InvalidEnumArgumentException(el.Name.LocalName);
                        }
                    }

                    GiftCardInfo = giftCardInfo;
                    var giftCardInfos = new List<string>
                    {
                        GiftCardInfo.RecipientName,
                        GiftCardInfo.RecipientEmail,
                        GiftCardInfo.SenderName,
                        GiftCardInfo.SenderEmail,
                        GiftCardInfo.Message
                    };

                    map.AddRange(0, giftCardInfos);

                }
                catch (Exception ex)
                {
                    throw new XmlException("Error while trying to parse from additional XML: " + nameof(ProductVariantAttributeSelection), ex);
                }
            }
        }

        protected override void ToAdditionalXml(XElement root)
        {
            if (GiftCardInfo is null)
                return;

            var xElement = new XElement("GiftCardInfo");
            xElement.Add(new XElement(nameof(GiftCardInfo.RecipientName), GiftCardInfo.RecipientName));
            xElement.Add(new XElement(nameof(GiftCardInfo.RecipientEmail), GiftCardInfo.RecipientEmail));
            xElement.Add(new XElement(nameof(GiftCardInfo.SenderName), GiftCardInfo.SenderName));
            xElement.Add(new XElement(nameof(GiftCardInfo.SenderEmail), GiftCardInfo.SenderEmail));
            xElement.Add(new XElement(nameof(GiftCardInfo.Message), GiftCardInfo.Message));

            root.Add(xElement);
        }
    }
}