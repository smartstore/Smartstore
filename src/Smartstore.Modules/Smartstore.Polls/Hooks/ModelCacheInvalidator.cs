using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Polls.Domain;

namespace Smartstore.Polls.Hooks
{
    internal partial class ModelCacheInvalidator : IDbSaveHook
    {
        /// <summary>
        /// Key for home page polls
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// {1} : current store ID
        /// </remarks>
        public const string HOMEPAGE_POLLS_MODEL_KEY = "pres:poll:homepage-{0}-{1}";
        /// <summary>
        /// Key for polls by system name
        /// </summary>
        /// <remarks>
        /// {0} : poll system name
        /// {1} : language ID
        /// {2} : current store ID
        /// </remarks>
        public const string POLL_BY_SYSTEMNAME_MODEL_KEY = "pres:poll:systemname-{0}-{1}-{2}";
        public const string POLLS_PATTERN_KEY = "pres:poll:*";

        private readonly ICacheManager _cache;

        public ModelCacheInvalidator(ICacheManager cache)
        {
            _cache = cache;
        }

        public Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Void);

        public async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = entry.Entity;
            var result = HookResult.Ok;

            if (entity is Poll)
            {
                await _cache.RemoveByPatternAsync(POLLS_PATTERN_KEY);
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
    }
}
