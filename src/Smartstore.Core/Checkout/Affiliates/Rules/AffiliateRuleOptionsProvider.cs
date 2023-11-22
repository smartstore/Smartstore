using Smartstore.Core.Data;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Checkout.Affiliates.Rules
{
    public partial class AffiliateRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public AffiliateRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.Affiliate;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.Affiliate)
            {
                return null;
            }

            var result = new RuleOptionsResult();

            var pager = _db.Affiliates
                .AsNoTracking()
                .Include(x => x.Address)
                .Where(x => x.Active)
                .ToFastPager();

            while ((await pager.ReadNextPageAsync<Affiliate>()).Out(out var affiliates))
            {
                result.AddOptions(context, affiliates.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.Address?.GetFullName()?.NullEmpty() ?? StringExtensions.NotAvailable,
                    Hint = x.Address?.Email
                }));
            }

            return result;
        }
    }
}
