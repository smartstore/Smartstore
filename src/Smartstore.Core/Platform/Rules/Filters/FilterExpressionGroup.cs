using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Smartstore.Core.Rules.Filters
{
    public class FilterExpressionGroup : FilterExpression, IRuleExpressionGroup
    {
        private readonly List<IRuleExpression> _expressions = new();

        internal FilterExpressionGroup(params FilterExpression[] expressions)
            : this(null, expressions)
        {
        }

        public FilterExpressionGroup(Type entityType, params FilterExpression[] expressions)
        {
            EntityType = entityType;
            LogicalOperator = LogicalRuleOperator.And;
            Operator = RuleOperator.IsEqualTo;
            Value = true;

            _expressions.AddRange(expressions);
        }

        public int RefRuleId { get; set; }
        public Type EntityType { get; internal set; }
        public LogicalRuleOperator LogicalOperator { get; set; }
        public bool IsSubGroup { get; set; }
        public IRuleProvider Provider { get; set; }

        public IEnumerable<IRuleExpression> Expressions => _expressions;

        public void AddExpressions(IEnumerable<IRuleExpression> expressions)
        {
            Guard.NotNull(expressions, nameof(expressions));
            _expressions.AddRange(expressions.OfType<FilterExpression>());
        }

        public Expression ToPredicate(IQueryProvider provider)
            => ToPredicate(null, provider);

        public override Expression ToPredicate(ParameterExpression node, IQueryProvider provider)
        {
            if (node == null)
            {
                if (EntityType == null)
                {
                    throw new InvalidOperationException($"{nameof(EntityType)} must not be null if 'ToPredicate()' method is called with '{nameof(node)}' parameter being null.");
                }

                node = Expression.Parameter(EntityType, "it");
            } 

            // TODO: was base.Descriptor.EntityType, check if MemberExpression is the same
            return ExpressionHelper.CreateLambdaExpression(node,  CreateBodyExpression(node, provider));
        }

        protected override Expression CreateBodyExpression(ParameterExpression node, IQueryProvider provider)
        {
            Expression left = null;

            foreach (var expression in Expressions.Cast<FilterExpression>().ToArray())
            {
                var right = expression.ToPredicate(node, provider);

                if (left == null)
                    left = right;
                else
                    left = ExpressionHelper.CombineExpressions(left, expression.LogicalOperator ?? LogicalOperator, right);
            }

            if (left == null)
            {
                return ExpressionHelper.TrueLiteral;
            }

            return left;
        }
    }
}
