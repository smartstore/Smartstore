using System.Collections.Generic;

namespace Smartstore.Core.Checkout.Shipping
{
    /// <summary>
    /// Represents a response from get shipping options
    /// </summary>
    public partial class GetShippingOptionResponse
    {
        /// <summary>
        /// Gets shipping options
        /// </summary>
        public List<ShippingOption> ShippingOptions { get; init; } = new();

        /// <summary>
        /// Gets errors
        /// </summary>
        public List<string> Errors { get; init; } = new();

        /// <summary>
        /// Gets a value indicating whether the response is successful
        /// </summary>
        public bool Success 
            => Errors.Count == 0;

        /// <summary>
        /// Adds an error to <see cref="Errors"/>
        /// </summary>
        /// <param name="error">Error message</param>
        public void AddError(string error) 
            => Errors.Add(error);
    }
}
