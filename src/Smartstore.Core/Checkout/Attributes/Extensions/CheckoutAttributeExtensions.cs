using System.Collections.Generic;
using System.Linq;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute extensions.
    /// </summary>
    public static partial class CheckoutAttributeExtensions
    {
        /// <summary>
        /// Gets invalid shippable attribute ids from <paramref name="attributes"/>.
        /// </summary>
        /// <returns><see cref="IEnumerable{int}"/> with invalid shippable attribute identifiers.</returns>
        public static IEnumerable<int> GetInvalidShippableAttributesIds(this IEnumerable<CheckoutAttribute> attributes)
        {
            Guard.NotNull(attributes, nameof(attributes));

            return attributes
                .Where(x => x.ShippableProductRequired)
                .Select(x => x.Id);
        }
    }
}