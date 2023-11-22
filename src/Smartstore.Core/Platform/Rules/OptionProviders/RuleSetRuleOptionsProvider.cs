using Smartstore.Core.Data;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Rules
{
    public partial class RuleSetRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public RuleSetRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.CartRule || dataSource == KnownRuleOptionDataSourceNames.TargetGroup;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.CartRule && context.DataSource != KnownRuleOptionDataSourceNames.TargetGroup)
            {
                return null;
            }

            if (context.Reason == RuleOptionsRequestReason.SelectedDisplayNames)
            {
                var ruleSets = await _db.RuleSets.GetManyAsync(context.Value.ToIntArray());
                var options = ruleSets.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                });

                return RuleOptionsResult.Create(context, options);
            }
            else
            {
                var ruleSets = await _db.RuleSets
                    .AsNoTracking()
                    .ApplyStandardFilter(context.Descriptor.Scope, false, true)
                    .ToPagedList(context.PageIndex, context.PageSize)
                    .LoadAsync();

                var options = ruleSets.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                });

                return RuleOptionsResult.Create(context, options, true, ruleSets.HasNextPage);
            }
        }
    }
}
