using Autofac;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules
{
    public class AttributeRuleProvider : RuleProviderBase, IAttributeRuleProvider
    {
        private readonly SmartDbContext _db;
        private readonly IComponentContext _componentContext;
        private readonly IRuleService _ruleService;

        public AttributeRuleProvider(
            SmartDbContext db,
            IComponentContext componentContext,
            IRuleService ruleService)
            : base(RuleScope.ProductAttribute)
        {
            _db = db;
            _componentContext = componentContext;
            _ruleService = ruleService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public IRule<AttributeRuleContext> GetProcessor(RuleExpression expression)
        {
            var group = expression as RuleExpressionGroup;
            var descriptor = expression.Descriptor as AttributeRuleDescriptor;

            if (group == null && descriptor == null)
            {
                throw new InvalidOperationException($"Missing attribute rule descriptor for expression {expression.Id} ('{expression.RawValue.EmptyNull()}').");
            }

            IRule<AttributeRuleContext> instance;

            if (group == null && descriptor.ProcessorType != typeof(AttributeCompositeRule))
            {
                instance = _componentContext.ResolveKeyed<IRule<AttributeRuleContext>>(descriptor.ProcessorType);
            }
            else
            {
                instance = new AttributeCompositeRule(group, this);
            }

            return instance;
        }

        public override async Task<IRuleExpression> VisitRuleAsync(RuleEntity rule)
        {
            var expression = new RuleExpression();
            await base.ConvertRuleAsync(rule, expression);
            return expression;
        }

        public override IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet)
        {
            var group = new RuleExpressionGroup
            {
                Id = ruleSet.Id,
                LogicalOperator = ruleSet.LogicalOperator,
                IsSubGroup = ruleSet.IsSubGroup,
                Value = ruleSet.Id,
                RawValue = ruleSet.Id.ToString(),
                Provider = this,
                Descriptor = new AttributeRuleDescriptor
                {
                    RuleType = RuleType.Boolean,
                    ProcessorType = typeof(AttributeCompositeRule)
                }
            };

            return group;
        }

        public async Task<bool> RuleMatchesAsync(
            ProductVariantAttribute attribute,
            LogicalRuleOperator logicalOperator,
            Action<AttributeRuleContext> contextAction = null)
        {
            if (attribute?.RuleSetId == null)
            {
                return true;
            }

            await _db.LoadReferenceAsync(attribute, x => x.RuleSet, false, q => q.Include(x => x.Rules));

            var group = await _ruleService.CreateExpressionGroupAsync(attribute.RuleSet, this);
            if (group == null)
            {
                return true;
            }

            var expressions = group.Expressions
                .Select(x => x as RuleExpression)
                .Where(x => x != null)
                .ToArray();

            return await RuleMatchesAsync(expressions, logicalOperator, contextAction);
        }

        public async Task<bool> RuleMatchesAsync(
            RuleExpression[] expressions,
            LogicalRuleOperator logicalOperator,
            Action<AttributeRuleContext> contextAction = null)
        {
            Guard.NotNull(expressions);

            if (expressions.Length == 0)
            {
                return true;
            }

            RuleExpressionGroup group;

            if (expressions.Length == 1 && expressions[0] is RuleExpressionGroup group2)
            {
                group = group2;
            }
            else
            {
                group = new RuleExpressionGroup { LogicalOperator = logicalOperator };
                group.AddExpressions(expressions);
            }

            var context = new AttributeRuleContext
            {
            };

            contextAction?.Invoke(context);

            var processor = GetProcessor(group);
            var result = await processor.MatchAsync(context, group);

            return result;
        }

        protected override Task<IEnumerable<RuleDescriptor>> LoadDescriptorsAsync()
        {
            var descriptors = new List<AttributeRuleDescriptor>
            {
            };

            return Task.FromResult(descriptors.Cast<RuleDescriptor>());
        }

    }
}
