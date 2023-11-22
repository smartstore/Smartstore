using Smartstore.Core.Data;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Localization.Rules
{
    public partial class LanguageRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public LanguageRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.Language;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.Language)
            {
                return null;
            }

            var languages = await _db.Languages
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .ToListAsync();

            var options = languages.Select(x => new RuleValueSelectListOption
            {
                Value = x.Id.ToString(),
                Text = x.GetLocalized(x => x.Name)
            });

            return RuleOptionsResult.Create(context, options);
        }
    }
}
