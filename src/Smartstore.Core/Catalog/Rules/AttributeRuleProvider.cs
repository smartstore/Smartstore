using Autofac;
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
        IRuleService ruleService) 
        : RuleProviderBase(RuleScope.ProductAttribute), IAttributeRuleProvider
    {
        private readonly AttributeRuleProviderContext _context = context;
        private readonly SmartDbContext _db = db;
        private readonly IWorkContext _workContext = workContext;
        private readonly IComponentContext _componentContext = componentContext;
        private readonly IRuleService _ruleService = ruleService;

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

            if (attribute?.RuleSetId == null)
            {
                return VisitRuleSet(null);
            }

            await _db.LoadReferenceAsync(attribute, x => x.RuleSet, false, q => q.Include(x => x.Rules));

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

            if (context.Attribute?.RuleSetId == null)
            {
                return true;
            }

            await _db.LoadReferenceAsync(context.Attribute, x => x.RuleSet, false, q => q.Include(x => x.Rules));

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

        protected override Task<IEnumerable<RuleDescriptor>> LoadDescriptorsAsync()
        {
            // TODO: caching per request

            var descriptors = new List<AttributeRuleDescriptor>();
            var language = _workContext.WorkingLanguage;
            var attributeSelectedRuleType = typeof(ProductAttributeSelectedRule);

            foreach (var attribute in _context.AllAttributes.Where(x => x.Id != _context.Attribute.Id))
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

            //var query = _db.ProductAttributes.AsNoTracking().OrderBy(x => x.DisplayOrder);
            //var pageIndex = -1;

            //while (true)
            //{
            //    var variants = await query.ToPagedList(++pageIndex, 1000).LoadAsync();
            //    foreach (var variant in variants)
            //    {
            //        var descriptor = new AttributeRuleDescriptor
            //        {
            //            Name = $"Variant{variant.Id}",
            //            DisplayName = variant.GetLocalized(x => x.Name, language, true, false),
            //            //GroupKey = "Admin.Catalog.Attributes.ProductAttributes",
            //            RuleType = RuleType.IntArray,
            //            ProcessorType = attributeSelectedRuleType,
            //            // TODO: operators depends... IsComparingSequences = ProductVariantAttribute.IsMultipleChoice.
            //            //Operators = RuleType.IntArray.GetValidOperators(IsComparingSequences).ToArray();
            //            SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.VariantValue) { Multiple = true }
            //        };
            //        descriptor.Metadata["ParentId"] = variant.Id;
            //        descriptor.Metadata["ValueType"] = ProductVariantAttributeValueType.Simple;

            //        descriptors.Add(descriptor);
            //    }

            //    if (!variants.HasNextPage)
            //    {
            //        break;
            //    }
            //}

            return Task.FromResult(descriptors.Cast<RuleDescriptor>());
        }
    }
}
