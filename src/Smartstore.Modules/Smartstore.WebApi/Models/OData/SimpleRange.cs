namespace Smartstore.Web.Api.Models
{
    /// <summary>
    /// Represents a simple value range.
    /// </summary>
    public partial class SimpleRange<T>
    {
        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        public T Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        public T Maximum { get; set; }
    }
}
