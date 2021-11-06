using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Domain;
using Smartstore.Utilities;

namespace Smartstore.Caching.OutputCache
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

        IEnumerable<string> GetCacheControlTagsFor(BaseEntity entity);

        IEnumerable<string> GetAllCacheControlTags();

        IDisposable BeginIdleScope();
    }


    public static class IDisplayControlExtensions
    {
        public static void AnnounceRange(this IDisplayControl displayedEntities, IEnumerable<BaseEntity> entities)
        {
            if (entities != null)
            {
                entities.Each(x => displayedEntities.Announce(x));
            }
        }
    }

    public class NullDisplayControl : IDisplayControl
    {
        public bool IsUncacheableRequest => false;
        public void Announce(BaseEntity entity) { }
        public IDisposable BeginIdleScope() => new ActionDisposable();
        public IEnumerable<string> GetAllCacheControlTags() => Enumerable.Empty<string>();
        public IEnumerable<string> GetCacheControlTagsFor(BaseEntity entity) => Enumerable.Empty<string>();
        public bool IsDisplayed(BaseEntity entity) => false;
        public void MarkRequestAsUncacheable() { }
    }
}
