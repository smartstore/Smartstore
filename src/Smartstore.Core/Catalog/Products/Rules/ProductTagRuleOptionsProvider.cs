using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductTagRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public ProductTagRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.ProductTag;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.ProductTag)
            {
                return null;
            }

            var result = new RuleOptionsResult();
            var pager = _db.ProductTags.AsNoTracking().ToFastPager();

            while ((await pager.ReadNextPageAsync<ProductTag>()).Out(out var tags))
            {
                result.AddOptions(context, tags.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.GetLocalized(y => y.Name, context.Language, true, false)
                }));
            }

            return result;
        }
    }
}
