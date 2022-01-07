namespace Smartstore.Core.Checkout.Shipping
{
    /// <summary>
    /// Get shipping option request extensions
    /// </summary>
    public static partial class ShippingOptionRequestExtensions
    {
        /// <summary>
        /// Gets total width of <see cref="ShippingOptionRequest.Items"/>
        /// </summary>
        /// <returns>Total width</returns>
        public static decimal GetTotalWidth(this ShippingOptionRequest request)
        {
            Guard.NotNull(request, nameof(request));

            return request.Items
                .Where(x => x.Item.Product != null)
                .Select(x => x.Item.Quantity * x.Item.Product.Width)
                .Sum();
        }

        /// <summary>
        /// Gets total length of <see cref="ShippingOptionRequest.Items"/>
        /// </summary>
        /// <returns>Total length</returns>
        public static decimal GetTotalLength(this ShippingOptionRequest request)
        {
            Guard.NotNull(request, nameof(request));

            return request.Items
                .Where(x => x.Item.Product != null)
                .Select(x => x.Item.Quantity * x.Item.Product.Length)
                .Sum();
        }

        /// <summary>
        /// Gets total height of <see cref="ShippingOptionRequest.Items"/>
        /// </summary>
        /// <returns>Total height</returns>
        public static decimal GetTotalHeight(this ShippingOptionRequest request)
        {
            Guard.NotNull(request, nameof(request));

            return request.Items
                .Where(x => x.Item.Product != null)
                .Select(x => x.Item.Quantity * x.Item.Product.Height)
                .Sum();
        }
    }
}