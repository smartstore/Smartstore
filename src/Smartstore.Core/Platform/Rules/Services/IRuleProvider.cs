namespace Smartstore.Core.Rules
{
    /// <summary>
    /// Rule provider interface.
    /// </summary>
    public interface IRuleProvider : IRuleVisitor
    {
        /// <summary>
        /// Gets all rule descriptors for a rule scope.
        /// </summary>
        /// <returns></returns>
        Task<RuleDescriptorCollection> GetRuleDescriptorsAsync();
    }

    public abstract class RuleProviderBase : IRuleProvider
    {
        private RuleDescriptorCollection _descriptors;

        protected RuleProviderBase(RuleScope scope)
        {
            Scope = scope;
        }

        public RuleScope Scope { get; protected set; }

        public abstract Task<IRuleExpression> VisitRuleAsync(RuleEntity rule);

        public abstract IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet);

        protected virtual async Task ConvertRuleAsync(RuleEntity entity, RuleExpression expression)
        {
            Guard.NotNull(entity);
            Guard.NotNull(expression);

            var descriptors = await GetRuleDescriptorsAsync();
            var descriptor = descriptors.FindDescriptor(entity.RuleType);
            if (descriptor == null)
            {
                // A descriptor for this entity data does not exist. Allow deletion of it.
                descriptor = new InvalidRuleDescriptor(Scope)
                {
                    Name = entity.RuleType,
                    DisplayName = entity.RuleType
                };
            }
            else if (descriptor.Scope != Scope)
            {
                throw new InvalidOperationException($"Differing rule scope {descriptor.Scope}. Expected {Scope}.");
            }

            expression.Id = entity.Id;
            expression.RuleSetId = entity.RuleSetId;
            expression.Descriptor = descriptor;
            expression.Operator = entity.Operator;
            expression.RawValue = entity.Value;
            expression.Value = entity.Value.Convert(descriptor.RuleType.ClrType);
        }

        public virtual async Task<RuleDescriptorCollection> GetRuleDescriptorsAsync()
        {
            if (_descriptors == null)
            {
                var descriptors = await LoadDescriptorsAsync();
                _descriptors = new RuleDescriptorCollection(descriptors);
            }

            return _descriptors;
        }

        protected abstract Task<IEnumerable<RuleDescriptor>> LoadDescriptorsAsync();
    }
}
