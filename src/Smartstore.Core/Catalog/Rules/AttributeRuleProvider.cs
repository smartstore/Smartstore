using System.Text;
using Autofac;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Rules.Impl;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules
{
    public class AttributeRuleProvider(
        AttributeRuleProviderContext context,
        SmartDbContext db,
        IWorkContext workContext,
        IComponentContext componentContext,
        IRuleService ruleService,
        IRequestCache requestCache)
        : RuleProviderBase(RuleScope.ProductAttribute), IAttributeRuleProvider
    {
        // {0} = ProductVariantAttribute.Id
        private readonly static CompositeFormat DescriptorsByAttributeIdKey = CompositeFormat.Parse("ruledescriptors:byattribute-{0}");

        private readonly AttributeRuleProviderContext _ctx = context;
        private readonly SmartDbContext _db = db;
        private readonly IWorkContext _workContext = workContext;
        private readonly IComponentContext _componentContext = componentContext;
        private readonly IRuleService _ruleService = ruleService;
        private readonly IRequestCache _requestCache = requestCache;

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

        public async Task<IRuleExpressionGroup> CreateExpressionGroupAsync(ProductVariantAttribute attribute, bool includeHidden = false)
        {
            Guard.NotNull(attribute);

            await _db.LoadReferenceAsync(attribute, x => x.RuleSet, false, q => q.Include(x => x.Rules));

            if (attribute.RuleSet == null)
            {
                return VisitRuleSet(null);
            }

            var group = await _ruleService.CreateExpressionGroupAsync(attribute.RuleSet, this, includeHidden);
            await _ruleService.ApplyMetadataAsync(group);

            return group;
        }

        public override async Task<IRuleExpression> VisitRuleAsync(RuleEntity rule)
        {
            var expression = new RuleExpression();
            await base.ConvertRuleAsync(rule, expression);
            return expression;
        }

        public override IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet)
        {
            return new RuleExpressionGroup
            {
                Id = ruleSet?.Id ?? 0,
                LogicalOperator = ruleSet?.LogicalOperator ?? LogicalRuleOperator.And,
                IsSubGroup = ruleSet?.IsSubGroup ?? false,
                Value = ruleSet?.Id ?? 0,
                RawValue = ruleSet?.Id.ToString() ?? "0",
                Provider = this,
                Descriptor = new AttributeRuleDescriptor
                {
                    RuleType = RuleType.Boolean,
                    ProcessorType = typeof(AttributeCompositeRule)
                }
            };
        }

        public async Task<bool> IsAttributeActiveAsync(AttributeRuleContext context, LogicalRuleOperator logicalOperator = LogicalRuleOperator.And)
        {
            Guard.NotNull(context);

            await _db.LoadReferenceAsync(context.Attribute, x => x.RuleSet, false, q => q.Include(x => x.Rules));

            if (context.Attribute.RuleSet == null)
            {
                return true;
            }

            var rules = await _ruleService.CreateExpressionGroupAsync(context.Attribute.RuleSet, this);

            var expressions = rules?.Expressions
                ?.Select(x => x as RuleExpression)
                ?.Where(x => x != null)
                ?.ToArray();

            if (expressions.IsNullOrEmpty())
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

            var processor = GetProcessor(group);
            var result = await processor.MatchAsync(context, group);

            return result;
        }

        protected override async Task<IEnumerable<RuleDescriptor>> LoadDescriptorsAsync()
        {
            var result = await _requestCache.GetAsync(DescriptorsByAttributeIdKey.FormatInvariant(_ctx.AttributeId), async () =>
            {
                var descriptors = new List<AttributeRuleDescriptor>();
                var attributeSelectedRuleType = typeof(ProductAttributeSelectedRule);
                var language = _workContext.WorkingLanguage;

                var attributes = _ctx.Attributes ?? await _db.ProductVariantAttributes
                    .AsNoTracking()
                    .Include(x => x.ProductAttribute)
                    .Include(x => x.ProductVariantAttributeValues)
                    .Where(x => x.ProductId == _db.ProductVariantAttributes.FirstOrDefault(x => x.Id == _ctx.AttributeId).ProductId)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync();

                foreach (var attribute in attributes.Where(x => x.Id != _ctx.AttributeId))
                {
                    var values = attribute.ProductVariantAttributeValues
                        .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.GetLocalized(x => x.Name, language, true, false) })
                        .ToArray();

                    descriptors.Add(new()
                    {
                        Name = $"Variant{attribute.Id}",
                        DisplayName = attribute.ProductAttribute.GetLocalized(x => x.Name, language, true, false),
                        //GroupKey = "Admin.Catalog.Attributes.ProductAttributes",
                        RuleType = RuleType.IntArray,
                        ProcessorType = attributeSelectedRuleType,
                        IsComparingSequences = attribute.IsMultipleChoice,
                        SelectList = new LocalRuleValueSelectList(values) { Multiple = true }
                    });
                }

                return descriptors.Cast<RuleDescriptor>();
            });

            return result;
        }
    }

    public partial class AttributeRuleProviderContext(int attributeId, List<ProductVariantAttribute> attributes = null)
    {
        public int AttributeId { get; } = Guard.NotZero(attributeId);
        public List<ProductVariantAttribute> Attributes { get; } = attributes;
    }
}
