using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Rules
{
    internal class RuleSetHook : AsyncDbSaveHook<RuleSetEntity>
    {
        private readonly SmartDbContext _db;
        private readonly HashSet<int> _subRuleSetIdsToDelete = new();

        public RuleSetHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override async Task<HookResult> OnDeletingAsync(RuleSetEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await GetSubRuleSetIds([entity.Id], cancelToken);

            return HookResult.Ok;
        }

        protected override Task<HookResult> OnDeletedAsync(RuleSetEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_subRuleSetIdsToDelete.Count > 0)
            {
                // Delete orphaned sub rule sets.
                // May occur if a parent that has descendant sub rule set(s) has been deleted.
                await _db.RuleSets
                    .Where(x => _subRuleSetIdsToDelete.Contains(x.Id))
                    .ExecuteDeleteAsync(cancelToken);

                _subRuleSetIdsToDelete.Clear();
            }
        }

        private async Task GetSubRuleSetIds(int[] ruleSetIds, CancellationToken cancelToken)
        {
            var rawSubRuleSetIds = await _db.Rules
                .Where(x => ruleSetIds.Contains(x.RuleSetId) && x.RuleType == "Group" && x.RuleSet.IsSubGroup)
                .Select(x => x.Value)
                .ToArrayAsync(cancelToken);

            var subRuleSetIds = rawSubRuleSetIds
                .Select(x => x.ToInt())
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            if (subRuleSetIds.Length > 0)
            {
                _subRuleSetIdsToDelete.AddRange(subRuleSetIds);
                await GetSubRuleSetIds(subRuleSetIds, cancelToken);
            }
        }
    }
}
