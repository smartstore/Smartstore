using Smartstore.Core.Rules;

namespace Smartstore.Admin.Models.Rules
{
    public class RuleListModel : ModelBase
    {
        public RuleScope? Scope { get; set; }
        public string ScopeName { get; set; }
    }
}
