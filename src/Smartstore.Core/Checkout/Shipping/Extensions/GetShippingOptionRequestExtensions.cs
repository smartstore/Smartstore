using System.Linq;

namespace Smartstore.Core.Checkout.Shipping.Extensions
{
    /// <summary>
    /// Get shipping option request extensions
    /// </summary>
    public static class GetShippingOptionRequestExtensions
    {
        /// <summary>
        /// Gets total width of <see cref="GetShippingOptionRequest.Items"/>
        /// </summary>
        /// <returns>Total width</returns>
        public static decimal GetTotalWidth(this GetShippingOptionRequest request)
        {
            Guard.NotNull(request, nameof(request));

            return request.Items
                .Where(x => x.Item.Product != null)
                .Select(x => x.Item.Quantity * x.Item.Product.Width)
                .Sum();
        }

        /// <summary>
        /// Gets total length of <see cref="GetShippingOptionRequest.Items"/>
        /// </summary>
        /// <returns>Total length</returns>
        public static decimal GetTotalLength(this GetShippingOptionRequest request)
        {
            Guard.NotNull(request, nameof(request));

            return request.Items
                .Where(x => x.Item.Product != null)
                .Select(x => x.Item.Quantity * x.Item.Product.Length)
                .Sum();
        }

        /// <summary>
        /// Gets total height of <see cref="GetShippingOptionRequest.Items"/>
        /// </summary>
        /// <returns>Total height</returns>
        public static decimal GetTotalHeight(this GetShippingOptionRequest request)
        {
            Guard.NotNull(request, nameof(request));

            return request.Items
                .Where(x => x.Item.Product != null)
                .Select(x => x.Item.Quantity * x.Item.Product.Height)
                .Sum();
        }
    }
}