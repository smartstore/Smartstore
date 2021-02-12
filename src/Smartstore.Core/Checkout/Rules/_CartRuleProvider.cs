using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Autofac;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Rules
{
    public class CartRuleProvider : RuleProviderBase, ICartRuleProvider
    {
        private readonly IComponentContext _componentContext;
        //private readonly IRuleFactory _ruleFactory;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        public CartRuleProvider(
            IComponentContext componentContext,
            IWorkContext workContext,
            IStoreContext storeContext)
            : base(RuleScope.Cart)
        {
            _componentContext = componentContext;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public IRule GetProcessor(RuleExpression expression)
        {
            var group = expression as RuleExpressionGroup;
            var descriptor = expression.Descriptor as CartRuleDescriptor;

            if (group == null && descriptor == null)
            {
                throw new InvalidOperationException($"Missing cart rule descriptor for expression {expression.Id} ('{expression.RawValue.EmptyNull()}').");
            }

            IRule instance;

            if (group == null && descriptor.ProcessorType != typeof(CompositeRule))
            {
                instance = _componentContext.ResolveKeyed<IRule>(descriptor.ProcessorType);
            }
            else
            {
                instance = new CompositeRule(group, this);
            }

            return instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RuleExpressionGroup CreateExpressionGroup(int ruleSetId)
        {
            // TODO: (mg) (core) Complete CartRuleProvider (IRuleFactory required).
            //return _ruleFactory.CreateExpressionGroup(ruleSetId, this) as RuleExpressionGroup;
            throw new NotImplementedException();
        }

        public override IRuleExpression VisitRule(RuleEntity rule)
        {
            var expression = new RuleExpression();
            base.ConvertRule(rule, expression);
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
                Descriptor = new CartRuleDescriptor
                {
                    RuleType = RuleType.Boolean,
                    ProcessorType = typeof(CompositeRule)
                }
            };

            return group;
        }

        public async Task<bool> RuleMatchesAsync(int[] ruleSetIds, LogicalRuleOperator logicalOperator)
        {
            Guard.NotNull(ruleSetIds, nameof(ruleSetIds));

            if (ruleSetIds.Length == 0)
            {
                return true;
            }

            // TODO: (mg) (core) Complete CartRuleProvider (IRuleFactory required).
            var expressions = ruleSetIds
                //.Select(id => _ruleFactory.CreateExpressionGroup(id, this))
                //.Where(x => x != null)
                .Cast<RuleExpression>()
                .ToArray();

            return await RuleMatchesAsync(expressions, logicalOperator);
        }

        public async Task<bool> RuleMatchesAsync(IRulesContainer entity, LogicalRuleOperator logicalOperator = LogicalRuleOperator.Or)
        {
            Guard.NotNull(entity, nameof(entity));

            var ruleSets = entity.RuleSets.Where(x => x.Scope == RuleScope.Cart).ToArray();
            if (!ruleSets.Any())
            {
                return true;
            }

            // TODO: (mg) (core) Complete CartRuleProvider (IRuleFactory required).
            var expressions = ruleSets
                //.Select(x => _ruleFactory.CreateExpressionGroup(x, this))
                //.Where(x => x != null)
                .Cast<RuleExpression>()
                .ToArray();

            return await RuleMatchesAsync(expressions, logicalOperator);
        }

        public async Task<bool> RuleMatchesAsync(RuleExpression[] expressions, LogicalRuleOperator logicalOperator)
        {
            Guard.NotNull(expressions, nameof(expressions));

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

            var context = new CartRuleContext(() => group.GetHashCode())
            {
                Customer = _workContext.CurrentCustomer,
                Store = _storeContext.CurrentStore,
                WorkContext = _workContext
            };

            var processor = GetProcessor(group);
            var result = await processor.MatchAsync(context, group);

            return result;
        }

        protected override IEnumerable<RuleDescriptor> LoadDescriptors()
        {
            var language = _workContext.WorkingLanguage;
            var currencyCode = _storeContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;

            var stores = _storeContext.GetAllStores()
                .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.Name })
                .ToArray();

            var descriptors = new List<CartRuleDescriptor>
            {
            };

            descriptors
                .Where(x => x.RuleType == RuleType.Money)
                .Each(x => x.Metadata["postfix"] = currencyCode);

            return descriptors;
        }
    }
}
