using System;
using Smartstore.Collections;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Attributes
{
    // TODO: (mg) (core) AttributeSelection doesn't support Gift Card attributes format!
    // E.g. <Attributes><GiftCardInfo><RecipientName>Max</RecipientName><RecipientEmail>maxmustermann@yahoo.de</RecipientEmail><SenderName>Erika</SenderName><SenderEmail>....

    /// <summary>
    /// Represents a product variant attribute selection.
    /// </summary>
    /// <remarks>
    /// This class can parse strings with XML or JSON format to <see cref="Multimap{TKey, TValue}"/> and vice versa.
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

        public IGiftCardAttributes GetGiftCardAttributes()
        {
            return null;
        }

        //private static GiftCardAttributes FromXml(string xmlAttributes)
        //{
        //    try
        //    {
        //        var xElement = XElement.Parse(xmlAttributes);
        //        var element = xElement.Descendants("GiftCardInfo").FirstOrDefault();
        //        if (element != null)
        //        {
        //            return new GiftCardAttributes
        //            {
        //                RecipientName = element.Element("RecipientName")?.Value,
        //                RecipientEmail = element.Element("RecipientEmail")?.Value,
        //                SenderName = element.Element("SenderName")?.Value,
        //                SenderEmail = element.Element("SenderEmail")?.Value,
        //                Message = element.Element("Message")?.Value
        //            };
        //        }

        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new XmlException("Error while trying to parse from gift card XML: " + xmlAttributes, ex);
        //    }
        //}
    }

    public interface IGiftCardAttributes
    {
        string RecipientName { get; }
        string RecipientEmail { get; }
        string SenderName { get; }
        string SenderEmail { get; }
        string Message { get; }
    }

    [Serializable]
    public class GiftCardAttributes : IGiftCardAttributes
    {
        public string RecipientName { get; set; }
        public string RecipientEmail { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string Message { get; set; }
    }
}