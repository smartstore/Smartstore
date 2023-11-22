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
            => dataSource == KnownRuleOptionDataSourceNames.CustomerRole;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.CustomerRole)
            {
                return null;
            }

            var customerRoles = await _db.CustomerRoles
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            var options = customerRoles.Select(x => new RuleValueSelectListOption
            {
                Value = x.Id.ToString(),
                Text = x.Name
            });

            return RuleOptionsResult.Create(context, options);
        }
    }
}
