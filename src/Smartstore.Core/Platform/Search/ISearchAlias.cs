namespace Smartstore.Core.Search
{
    /// <summary>
    /// Represents an entity which supports SEO friendly search alias.
    /// </summary>
    public interface ISearchAlias
    {
        int Id { get; set; }

        /// <summary>
        /// Gets or sets the SEO friendly search alias.
        /// </summary>
        string Alias { get; set; }
    }
}
