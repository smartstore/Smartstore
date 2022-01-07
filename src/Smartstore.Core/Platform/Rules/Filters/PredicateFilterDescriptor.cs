namespace Smartstore.Core.Rules.Filters
{
    public abstract class PredicateFilterDescriptor<T, TPredicate, TPredicateValue> : FilterDescriptor<T, IEnumerable<TPredicate>>
        where T : class
        where TPredicate : class
    {
        protected PredicateFilterDescriptor(
            string methodName,
            Expression<Func<T, IEnumerable<TPredicate>>> path,
            Expression<Func<TPredicate, TPredicateValue>> predicate)
            : base(path)
        {
            MethodName = methodName;
            PredicateExpression = predicate;
        }

        protected string MethodName { get; set; }
        public Expression<Func<TPredicate, TPredicateValue>> PredicateExpression { get; private set; }

        public override Expression GetExpression(RuleOperator op, Expression valueExpression, IQueryProvider provider)
        {
            // Create the Any()/All() lambda predicate (the part within parentheses)
            var predicate = ExpressionHelper.CreateLambdaExpression(
                Expression.Parameter(typeof(TPredicate), "it2"),
                op.GetExpression(PredicateExpression.Body, valueExpression, provider));

            var body = Expression.Call(
                typeof(Enumerable),
                MethodName,
                // .Any/All<TPredicate>()
                new[] { typeof(TPredicate) },
                // 0 = left collection path: x.Orders.selectMany(o => o.OrderItems)
                // 1 = right Any/All predicate: y => y.ProductId = 1
                new Expression[]
                {
                    MemberExpression.Body,
                    predicate
                });

            return body;
        }
    }

    public class AnyFilterDescriptor<T, TAny, TAnyValue> : PredicateFilterDescriptor<T, TAny, TAnyValue>
        where T : class
        where TAny : class
    {
        public AnyFilterDescriptor(
            Expression<Func<T, IEnumerable<TAny>>> path,
            Expression<Func<TAny, TAnyValue>> anyPredicate)
            : base("Any", path, anyPredicate)
        {
        }
    }

    public class AllFilterDescriptor<T, TAll, TAllValue> : PredicateFilterDescriptor<T, TAll, TAllValue>
        where T : class
        where TAll : class
    {
        public AllFilterDescriptor(
            Expression<Func<T, IEnumerable<TAll>>> path,
            Expression<Func<TAll, TAllValue>> allPredicate)
            : base("All", path, allPredicate)
        {
        }
    }
}
