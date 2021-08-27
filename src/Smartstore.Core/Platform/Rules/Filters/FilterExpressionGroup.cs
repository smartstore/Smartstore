using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Smartstore.Core.Rules.Filters
{
    public class FilterExpressionGroup : FilterExpression, IRuleExpressionGroup
    {
        private readonly List<IRuleExpression> _expressions = new();

        public FilterExpressionGroup(Type entityType)
        {
            Guard.NotNull(entityType, nameof(entityType));

            EntityType = entityType;
            LogicalOperator = LogicalRuleOperator.And;
            Operator = RuleOperator.IsEqualTo;
            Value = true;
        }

        public int RefRuleId { get; set; }
        public Type EntityType { get; private set; }
        public LogicalRuleOperator LogicalOperator { get; set; }
        public bool IsSubGroup { get; set; }
        public IRuleProvider Provider { get; set; }

        public IEnumerable<IRuleExpression> Expressions => _expressions;

        public void AddExpressions(params IRuleExpression[] expressions)
        {
            Guard.NotNull(expressions, nameof(expressions));
            _expressions.AddRange(expressions.OfType<FilterExpression>());
        }

        public Expression ToPredicate(IQueryProvider provider)
            => ToPredicate(null, provider);

        public override Expression ToPredicate(ParameterExpression node, IQueryProvider provider)
        {
            node ??= Expression.Parameter(EntityType, "it");

            // TODO: was base.Descriptor.EntityType, check if MemberExpression is the same
            return ExpressionHelper.CreateLambdaExpression(node,  CreateBodyExpression(node, provider));
        }

        protected override Expression CreateBodyExpression(ParameterExpression node, IQueryProvider provider)
        {
            var expressions = Expressions
                .Cast<FilterExpression>()
                .Select(x => x.ToPredicate(node, provider))
                .ToArray();

            return ExpressionHelper.CombineExpressions(node, LogicalOperator, expressions);
        }
    }
}
