using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Rules
{
    internal class RuleHook : AsyncDbSaveHook<RuleEntity>
    {
        private readonly SmartDbContext _db;
        private readonly HashSet<int> _ruleSetIdsOfDeletedRules = [];

        public RuleHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletingAsync(RuleEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            _ruleSetIdsOfDeletedRules.Add(entity.RuleSetId);

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnDeletedAsync(RuleEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_ruleSetIdsOfDeletedRules.Count > 0)
            {
                // Delete product attribute rule sets that has no rules.
                // In this case, the attribute is no longer conditional.
                await _db.RuleSets
                    .Where(x => _ruleSetIdsOfDeletedRules.Contains(x.Id) && x.Scope == RuleScope.ProductAttribute && !x.IsSubGroup && x.Rules.Count == 0)
                    .ExecuteDeleteAsync(cancelToken);

                _ruleSetIdsOfDeletedRules.Clear();
            }
        }
    }
}
