using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Polls.Domain;

namespace Smartstore.Polls.Hooks
{
    internal partial class ModelCacheInvalidator : AsyncDbSaveHook<Poll>
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
        /// {0} : poll id
        /// {1} : poll system name
        /// {2} : language ID
        /// {3} : current store ID
        /// </remarks>
        public const string POLL_BY_SYSTEMNAME_MODEL_KEY = "pres:poll:systemname-{0}-{1}-{2}-{3}";
        public const string POLLS_PATTERN_KEY = "pres:poll:*";

        private readonly ICacheManager _cache;

        public ModelCacheInvalidator(ICacheManager cache)
        {
            _cache = cache;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            await _cache.RemoveByPatternAsync(POLLS_PATTERN_KEY);
        }
    }
}
