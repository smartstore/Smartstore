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
        {
            return dataSource == KnownRuleOptionDataSourceNames.ProductTag;
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            var result = new RuleOptionsResult();

            if (context.DataSource == KnownRuleOptionDataSourceNames.ProductTag)
            {
                var productTags = await _db.ProductTags.AsNoTracking().ToListAsync();

                result.AddOptions(context, productTags
                    .Select(x => new RuleValueSelectListOption
                    {
                        Value = x.Id.ToString(),
                        Text = x.GetLocalized(y => y.Name, context.Language, true, false)
                    })
                    .OrderBy(x => x.Text));
            }
            else
            {
                return null;
            }

            return result;
        }
    }
}
