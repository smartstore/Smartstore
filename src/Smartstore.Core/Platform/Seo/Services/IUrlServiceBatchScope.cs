namespace Smartstore.Core.Seo
{
    /// <summary>
    /// Interface for batch optimized database interaction.
    /// </summary>
    public interface IUrlServiceBatchScope : IDisposable
    {
        /// <summary>
        /// Gets the active slug for an entity directly from the database, never from the cache
        /// to avoid high memory pressure.
        /// </summary>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="entityName">Entity name</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Found slug or empty string</returns>
        Task<string> GetActiveSlugAsync(int entityId, string entityName, int languageId);

        /// <summary>
        /// Adds slugs to the queue. No database call will be made.
        /// </summary>
        /// <param name="slugs">SLugs to add to the queue.</param>
        void ApplySlugs(params ValidateSlugResult[] slugs);

        /// <summary>
        /// Analyzes, validates and commits the quete to the database.
        /// </summary>
        /// <returns>The number of affected records.</returns>
        Task<int> CommitAsync(CancellationToken cancelToken = default);
    }
}
