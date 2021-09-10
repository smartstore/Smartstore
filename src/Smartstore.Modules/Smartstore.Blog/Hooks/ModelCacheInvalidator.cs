using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Blog.Domain;
using Smartstore.Caching;
using Smartstore.Core.Configuration;
using Smartstore.Data.Hooks;
using Smartstore.Utilities;

namespace Smartstore.Blog.Hooks
{
    internal partial class ModelCacheInvalidator : IDbSaveHook
    {
        /// <summary>
        /// Key for blog tag list model
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// {1} : store ID
        /// </remarks>
        public const string BLOG_TAGS_MODEL_KEY = "pres:blog:tags-{0}-{1}";
        /// <summary>
        /// Key for blog archive (years, months) block model
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// {1} : current store ID
        /// </remarks>
        public const string BLOG_MONTHS_MODEL_KEY = "pres:blog:months-{0}-{1}";
        public const string BLOG_PATTERN_KEY = "pres:blog:*";

        private readonly static string NumberOfTagsName = TypeHelper.NameOf<BlogSettings>(x => x.NumberOfTags, true);

        private readonly ICacheManager _cache;

        public ModelCacheInvalidator(ICacheManager cache)
        {
            _cache = cache;
        }

        public async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = entry.Entity;
            var result = HookResult.Ok;

            if (entity is BlogPost)
            {
                await _cache.RemoveByPatternAsync(BLOG_PATTERN_KEY);
            }
            else if (entity is Setting)
            {
                var setting = entity as Setting;
                
                if (setting.Name == NumberOfTagsName)
                {
                    await _cache.RemoveByPatternAsync(BLOG_PATTERN_KEY); // depends on BlogSettings.NumberOfTags
                }
            }
            else
            {
                // Register as void hook for all other entity type/state combis
                result = HookResult.Void;
            }

            return result;
        }

        public Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
            => Task.CompletedTask;

        public Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
            => Task.CompletedTask;

        public Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Void);
    }
}
