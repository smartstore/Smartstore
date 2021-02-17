using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Catalog.Brands
{
    public partial class ManufacturerRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public ManufacturerRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Ordinal => 0;

        public bool Matches(string dataSource)
        {
            return dataSource == KnownRuleOptionDataSourceNames.Manufacturer;
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            var result = new RuleOptionsResult();

            if (context.DataSource == KnownRuleOptionDataSourceNames.Manufacturer)
            {
                var manufacturers = await _db.Manufacturers
                    .AsNoTracking()
                    .ApplyStandardFilter(true)
                    .ToListAsync();

                result.AddOptions(context, manufacturers.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.GetLocalized(y => y.Name, context.Language, true, false)
                }));                    
            }
            else
            {
                return null;
            }

            return result;
        }
    }
}
