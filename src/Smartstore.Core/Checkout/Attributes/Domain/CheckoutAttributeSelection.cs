using Smartstore.Collections;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Represents a checkout attribute selection.
    /// </summary>
    /// <remarks>
    /// This class can parse strings with XML or JSON format to <see cref="Multimap{TKey, TValue}"/> and vice versa.
    /// </remarks>
    /// <remarks>
    /// Creates checkout attribute selection from string as <see langword="Multimap{int, object}"/>. 
    /// Use <see cref="AttributeSelection.AttributesMap"/> to access parsed attributes afterwards.
    /// </remarks>
    /// <remarks>
    /// Automatically differentiates between XML and JSON.
    /// </remarks>
    /// <param name="rawAttributes">XML or JSON attributes string.</param>  
    public class CheckoutAttributeSelection(string rawAttributes) : AttributeSelection(rawAttributes, "CheckoutAttribute")
    {
    }
}