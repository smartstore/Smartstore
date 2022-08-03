using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents the recurring cycle information
    /// </summary>
    public partial class RecurringCycleInfo
    {
        /// <summary>
        /// Gets or sets the cycle length
        /// </summary>
        public int? CycleLength { get; set; }

        /// <summary>
        /// Gets or sets total number of cycles
        /// </summary>
        public int? TotalCycles { get; set; }

        /// <summary>
        /// Gets or sets the recurring product cycle period
        /// </summary>
        public RecurringProductCyclePeriod? CyclePeriod { get; set; }

        /// <summary>
        /// Gets or sets an error message
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Gets a value indicating whether the nullable properties have already been assigned
        /// </summary>
        public bool HasValues
            => CycleLength.HasValue && TotalCycles.HasValue && CyclePeriod.HasValue;
    }
}