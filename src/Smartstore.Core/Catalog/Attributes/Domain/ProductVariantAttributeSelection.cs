using System;
using System.Collections.Generic;
using System.Linq;
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
    /// This class can parse strings with XML or JSON format to <see cref="Multimap{TKey, TValue}"/> and vice versa.
    /// </remarks>
    public class ProductVariantAttributeSelection : AttributeSelection
    {
        private static readonly Dictionary<string, int> _additionalKeyCodes = new()
        {
            { "GiftCardInfo", -10 }
        };

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

        protected override Dictionary<string, int> AdditionalKeyCodes
            => _additionalKeyCodes;

        protected override void MapElement(XElement element, Multimap<int, object> map)
        {
            if (element.Name.LocalName == "GiftCardInfo")
            {
                try
                {
                    var attributes = new List<string>();

                    var giftCardAttributes = new GiftCardInfo();
                    foreach (var el in element.Descendants())
                    {
                        giftCardAttributes.AddAttribute(el.Name.LocalName, el.Value);
                        //switch (el.Name.LocalName)
                        //{
                        //    case nameof(GiftCardAttributes.SenderName):
                        //        giftCardAttributes.SenderName = el.Value;
                        //}
                    }

                    if (giftCardAttributes.IsValidInfo())
                    {
                        GiftCardInfo = giftCardAttributes;
                        map.AddRange(AdditionalKeyCodes["GiftCardInfo"], giftCardAttributes.ToList());
                    }
                    else
                    {
                        throw new Exception("No valid gift card attribute info");
                    }
                }
                catch (Exception ex)
                {
                    throw new XmlException("Error while trying to parse from additional XML: " + nameof(ProductVariantAttributeSelection), ex);
                }
            }
        }

        protected override bool ToAdditionalXml(XElement root, KeyValuePair<int, ICollection<object>> attribute)
        {
            if (attribute.Key == AdditionalKeyCodes["GiftCardInfo"])
            {
                var xElement = new XElement("GiftCardInfo");
                var attributeValues = attribute.Value.Select(x => x.ToString()).ToList();
                var giftCardAttributes = new GiftCardInfo(attributeValues);

                foreach (var prop in giftCardAttributes.GetType().GetProperties())
                {
                    xElement.Add(new XElement(prop.Name, prop.GetValue(giftCardAttributes)));
                }

                root.Add(xElement);
                return true;
            }

            return false;
        }
    }
}