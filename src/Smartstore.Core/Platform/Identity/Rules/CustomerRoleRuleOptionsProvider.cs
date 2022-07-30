
using Smartstore.Core.Data;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Identity.Rules
{
    public partial class CustomerRoleRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public CustomerRoleRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
        {
            return dataSource == KnownRuleOptionDataSourceNames.CustomerRole;
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            var result = new RuleOptionsResult();

            if (context.DataSource == KnownRuleOptionDataSourceNames.CustomerRole)
            {
                var customerRoles = await _db.CustomerRoles
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                result.AddOptions(context, customerRoles.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
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
