using Smartstore.Core.Rules;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<RuleSetEntity> RuleSets { get; set; }
        public DbSet<RuleEntity> Rules { get; set; }
    }
}