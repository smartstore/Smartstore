using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Catalog.Brands.Rules
{
    public partial class ManufacturerRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public ManufacturerRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.Manufacturer;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.Manufacturer)
            {
                return null;
            }

            var manufacturers = await _db.Manufacturers
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .ToListAsync();

            var options = manufacturers.Select(x => new RuleValueSelectListOption
            {
                Value = x.Id.ToString(),
                Text = x.GetLocalized(y => y.Name, context.Language, true, false)
            });

            return RuleOptionsResult.Create(context, options);
        }
    }
}
