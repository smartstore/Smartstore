namespace Smartstore.Web.Api.Models
{
    /// <summary>
    /// Represents a simple value range.
    /// </summary>
    public partial class SimpleRange<T>
    {
        /// <summary>
        /// The minimum value.
        /// </summary>
        public T Minimum { get; set; }

        /// <summary>
        /// The maximum value.
        /// </summary>
        public T Maximum { get; set; }
    }
}
