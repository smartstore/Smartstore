namespace Smartstore.Core.OutputCache
{
    /// <summary>
    /// Responsible for collecting displayed entities during a request
    /// for automatic output cache invalidation.
    /// </summary>
    public interface IDisplayControl
    {
        /// <summary>
        /// Announces that the given <paramref name="entity"/> is about to be displayed.
        /// </summary>
        void Announce(BaseEntity entity);

        /// <summary>
        /// Checks whether the given <paramref name="entity"/> has been displayed (or will be displayed) during the current request.
        /// </summary>
        bool IsDisplayed(BaseEntity entity);

        /// <summary>
        /// Disables caching for the current request.
        /// </summary>
        void MarkRequestAsUncacheable();

        /// <summary>
        /// Checks whether the current request is uncacheable.
        /// </summary>
        bool IsUncacheableRequest { get; }

        Task<IEnumerable<string>> GetCacheControlTagsForAsync(BaseEntity entity);

        Task<string[]> GetAllCacheControlTagsAsync();

        IDisposable BeginIdleScope();
    }


    public static class IDisplayControlExtensions
    {
        public static void AnnounceRange(this IDisplayControl displayedEntities, IEnumerable<BaseEntity> entities)
        {
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    displayedEntities.Announce(entity);
                }
            }
        }
    }
}
