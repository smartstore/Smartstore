namespace Smartstore.Domain
{
    /// <summary>
    /// Represents an entity which supports paging options.
    /// </summary>
    public partial interface IPagingOptions
    {
        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        int? PageSize { get; }

        /// <summary>
        /// Gets or sets a value indicating whether customers can select the page size.
        /// </summary>
        bool? AllowCustomersToSelectPageSize { get; }

        /// <summary>
        /// Gets or sets the available page size options that the customer can select.
        /// </summary>
        string PageSizeOptions { get; }
    }
}
