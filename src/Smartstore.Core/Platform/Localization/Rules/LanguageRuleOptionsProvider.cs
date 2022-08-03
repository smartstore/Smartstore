using System.Globalization;
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
        {
            return dataSource == KnownRuleOptionDataSourceNames.Language;
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            var result = new RuleOptionsResult();

            if (context.DataSource == KnownRuleOptionDataSourceNames.Language)
            {
                var languages = await _db.Languages
                    .AsNoTracking()
                    .ApplyStandardFilter(true)
                    .ToListAsync();

                result.AddOptions(context, languages.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = GetCultureDisplayName(x) ?? x.Name
                }));
            }
            else
            {
                return null;
            }

            return result;
        }

        private static string GetCultureDisplayName(Language language)
        {
            if (language?.LanguageCulture?.HasValue() ?? false)
            {
                try
                {
                    return new CultureInfo(language.LanguageCulture).DisplayName;
                }
                catch { }
            }

            return null;
        }
    }
}
