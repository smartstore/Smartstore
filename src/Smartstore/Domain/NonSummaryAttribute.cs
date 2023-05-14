namespace Smartstore.Domain
{
    /// <summary>
    /// Used to annotate entity properties with potentially large data (such as long text, BLOB, etc.).
    /// Properties annotated with this attribute will be excluded or truncated when the 
    /// <c>SelectSummary</c> extension method is called to speed up the materialization of entity lists.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NonSummaryAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonSummaryAttribute"/> class.
        /// </summary>
        public NonSummaryAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NonSummaryAttribute"/> class with the specified field max length.
        /// </summary>
        /// <param name="maxLength">The length to truncate the field to.</param>
        public NonSummaryAttribute(int maxLength)
        {
            MaxLength = Guard.IsPositive(maxLength);
        }

        /// <summary>
        /// If <c>null</c>, property will be excluded from projection.
        /// If > 0, the first MaxLength chars with be read from field.
        /// </summary>
        public int? MaxLength { get; set; }
    }
}
