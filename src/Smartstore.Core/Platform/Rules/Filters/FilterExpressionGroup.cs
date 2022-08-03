namespace Smartstore.Core.Rules.Filters
{
    public class FilterExpressionGroup : FilterExpression, IRuleExpressionGroup
    {
        private List<IRuleExpression> _expressions = new();

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

            AddExpressions(expressions);
        }

        public int RefRuleId { get; set; }
        public Type EntityType { get; internal set; }
        public new LogicalRuleOperator LogicalOperator { get; set; }
        public bool IsSubGroup { get; set; }
        public IRuleProvider Provider { get; set; }

        public IEnumerable<IRuleExpression> Expressions => _expressions;

        public void AddExpressions(IEnumerable<IRuleExpression> expressions)
        {
            Guard.NotNull(expressions, nameof(expressions));

            _expressions.AddRange(expressions
                .OfType<FilterExpression>()
                .Select(GetSelfOrCloneIfGroup));
        }

        private static FilterExpression GetSelfOrCloneIfGroup(FilterExpression expression)
        {
            if (expression is FilterExpressionGroup group && !group.IsSubGroup)
            {
                var clone = (FilterExpressionGroup)group.MemberwiseClone();

                // This is why we have to clone
                clone.IsSubGroup = true;

                return clone;
            }

            return expression;
        }

        public Expression ToPredicate(IQueryProvider provider)
            => ToPredicate(null, provider);

        public override Expression ToPredicate(ParameterExpression node, IQueryProvider provider)
        {
            var bodyExpression = CreateBodyExpression(node, provider);

            if (!IsSubGroup && node == null)
            {
                if (EntityType == null)
                {
                    throw new InvalidOperationException($"For lambda expressions '{nameof(EntityType)}' must not be null if 'ToPredicate()' method is called with '{nameof(node)}' parameter being null.");
                }

                node = Expression.Parameter(EntityType, "it");
            }

            return IsSubGroup
                ? bodyExpression
                : ExpressionHelper.CreateLambdaExpression(node, bodyExpression);
        }

        protected override Expression CreateBodyExpression(ParameterExpression node, IQueryProvider provider)
        {
            Expression left = null;
            LogicalRuleOperator? op = null;

            foreach (var expression in Expressions.Cast<FilterExpression>().ToArray())
            {
                var right = expression.ToPredicate(node, provider);

                if (left == null)
                    left = right;
                else
                    left = ExpressionHelper.CombineExpressions(left, op ?? LogicalOperator, right);

                op = expression.LogicalOperator;
            }

            if (left == null)
            {
                return ExpressionHelper.TrueLiteral;
            }

            if (Value is bool value && value == false)
            {
                // Negate group
                return Expression.Equal(left, ExpressionHelper.FalseLiteral);
            }
            else
            {
                return left;
            }
        }
    }
}
