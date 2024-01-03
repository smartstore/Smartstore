#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Smartstore.Core.Rules
{
    public abstract class RuleDescriptor
    {
        private RuleOperator[]? _operators;

        protected RuleDescriptor(RuleScope scope)
        {
            Scope = scope;
        }

        public RuleScope Scope { get; }
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? GroupKey { get; set; }

        public bool IsValid => false == (this is InvalidRuleDescriptor);

        public RuleType RuleType { get; set; } = RuleType.Boolean;
        public RuleValueSelectList? SelectList { get; set; }

        /// <summary>
        /// Indicates whether the rule compares the values of two sequences.
        /// </summary>
        public bool IsComparingSequences { get; set; }

        public IEnumerable<IRuleConstraint> Constraints { get; set; } = Array.Empty<IRuleConstraint>();
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        public RuleOperator[] Operators
        {
            get => _operators ??= RuleType.GetValidOperators(IsComparingSequences).ToArray();
            set => _operators = value;
        }

        public T? GetMetadata<T>(string name, T? defaultValue = default)
            => TryGetMetadata<T>(name, out var val) ? val : defaultValue;

        public bool TryGetMetadata<T>(string name, [MaybeNullWhen(false)] out T? value)
        {
            if (Metadata.TryGetValue(name, out var raw))
            {
                value = raw != null ? raw.Convert<T>() : default;
                return true;
            }

            value = default;
            return false;
        }
    }


    public class InvalidRuleDescriptor : RuleDescriptor
    {
        public InvalidRuleDescriptor(RuleScope scope)
            : base(scope)
        {
            RuleType = RuleType.String;
        }
    }
}
