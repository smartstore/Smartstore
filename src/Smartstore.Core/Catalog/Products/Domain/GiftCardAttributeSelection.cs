using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Products
{
    // TODO: (mg) (core) Dude, that doesn't work. Gift card attributes format require a Multimap<string, object> mapping. Implement a new abstract class 'NamedAttributeSelection'.
    // E.g. <Attributes><GiftCardInfo><RecipientName>Max</RecipientName><RecipientEmail>maxmustermann@yahoo.de</RecipientEmail><SenderName>Erika</SenderName><SenderEmail>....

    /// <summary>
    /// Represents a gift card attribute selection.
    /// </summary>
    /// <remarks>
    /// This class can parse strings with XML or JSON format to <see cref="Multimap{TKey, TValue}"/> and vice versa.
    /// </remarks>
    public class GiftCardAttributeSelection : AttributeSelection
    {
        /// <summary>
        /// Creates gift card attribute selection from string as <see cref="Multimap{int, object}"/>. 
        /// Use <see cref="AttributeSelection.AttributesMap"/> to access parsed attributes afterwards.
        /// </summary>
        /// <remarks>
        /// Automatically differentiates between XML and JSON.
        /// </remarks>        
        /// <param name="rawAttributes">XML or JSON attributes string.</param>  
        public GiftCardAttributeSelection(string rawAttributes)
            : base(rawAttributes, "GiftCardInfo")
        {
        }
    }
}
