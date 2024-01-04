namespace Smartstore.Core.Rules
{
    public abstract class RuleDescriptor
    {
        private RuleOperator[] _operators;

        protected RuleDescriptor(RuleScope scope)
        {
            Scope = scope;
            Constraints = Array.Empty<IRuleConstraint>();
            Metadata = new Dictionary<string, object>();
        }

        public RuleScope Scope { get; protected set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string GroupKey { get; set; }

        public bool IsValid => false == (this is InvalidRuleDescriptor);

        public RuleType RuleType { get; set; }
        public RuleValueSelectList SelectList { get; set; }

        /// <summary>
        /// Indicates whether the rule compares the values of two sequences.
        /// </summary>
        public bool IsComparingSequences { get; set; }

        public IEnumerable<IRuleConstraint> Constraints { get; set; }
        public IDictionary<string, object> Metadata { get; }

        public RuleOperator[] Operators
        {
            get => _operators ??= RuleType.GetValidOperators(IsComparingSequences).ToArray();
            set => _operators = value;
        }
    }


    public class InvalidRuleDescriptor : RuleDescriptor
    {
        public InvalidRuleDescriptor(RuleScope scope)
            : base(scope)
        {
            RuleType = RuleType.String;
            Constraints = Array.Empty<IRuleConstraint>();
        }
    }
}