using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Domain;
using Smartstore.Utilities;

namespace Smartstore.Caching.OutputCache
{
    public interface IDisplayControl
    {
        void Announce(BaseEntity entity);

        bool IsDisplayed(BaseEntity entity);

        void MarkRequestAsUncacheable();

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
