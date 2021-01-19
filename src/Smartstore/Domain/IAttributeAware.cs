namespace Smartstore.Domain
{
    /// <summary>
    /// Represents an entity with attributes.
    /// </summary>
    public partial interface IAttributeAware
    {
        /// <summary>
        /// Gets or sets the raw attributes string in XML or JSON format.
        /// </summary>
        public string RawAttributes { get; set; }
    }
}