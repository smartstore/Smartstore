namespace Smartstore.Core.Rules.Filters
{
    public static class RuleQueryableExtensions
    {
        #region Common

        public static IQueryable Where(this IQueryable source, FilterExpression filter)
        {
            Expression predicate = filter.ToPredicate(null, source.Provider);
            return source.Where(predicate);
        }

        public static IQueryable Where(this IQueryable source, Expression predicate)
        {
            var typeArgs = new Type[] { source.ElementType };
            var exprArgs = new Expression[] { source.Expression, Expression.Quote(predicate) };

            return source.Provider.CreateQuery(
                Expression.Call(typeof(Queryable), "Where", typeArgs, exprArgs));
        }

        #endregion

        #region Entities

        /// <summary>
        /// Applies a complex string-based DSL filter to given entity member. Only numeric and string members are allowed.
        /// </summary>
        /// <param name="memberExpression">
        /// The member expression to apply filter to.
        /// </param>
        /// <param name="filter">
        ///     The search filter. Grammar:
        ///     <code>
        ///         <c>TERM:</c>
        ///             Quoted search term (double or single) | unquoted search term without whitespaces.
        ///             Supports wildcards (* | ?). If wildcards are present, default OPERATOR
        ///             is switched to "Equals". Use "NotEquals" (!) to negate pattern.
        ///             
        ///         <c>OPERATOR:</c>
        ///             =[=]    --> Equals (default when omitted on numeric terms)
        ///             ![=]    --> NotEquals
        ///             &gt;    --> GreaterThan
        ///             &gt;=   --> GreaterThanOrEqual
        ///             &lt;    --> LessThan
        ///             &lt;=   --> LessThanOrEqual
        ///             ~       --> Contains (default when omitted on string terms)
        ///             !~      --> NotContains
        ///             
        ///         <c>COMBINATOR:</c>
        ///             [and | or] (case-insensitive)
        ///             If omitted, "or" is used.
        ///             
        ///         <c>FILTER:</c>
        ///             [ OPERATOR ]TERM [COMBINATOR]
        ///             
        ///         <c>FILTER_GROUP:</c>
        ///             [!]([FILTER | FILTER_GROUP]*)
        ///             The optional "!" operator negates the group.
        ///             
        ///         <c>EXPRESSION:</c>
        ///             FILTER* | FILTER_GROUP*
        ///     </code>
        /// </param>
        /// <example>
        ///     <code>
        ///         <c>banana joe</c>
        ///             Contains "banana" or contains "joe"
        ///             
        ///         <c>banana and !*.joe</c>
        ///             Contains "banana" but does not match "*.joe"
        ///             
        ///         <c>~banana and (!~"hello world" or !*jim)</c>
        ///             Contains "banana", but does not contain "hello world" or does not end with "jim"
        ///             
        ///         <c>*Middleware and !(Serilog* Microsoft*)</c>
        ///             Ends with "Middleware", but does not starts with "Serilog" or "Microsoft"
        ///             
        ///         <c>(&gt;=10 and &lt;=100) or 1 or &gt;1000</c>
        ///             Is between 10 and 100, or equals 1, or is greater than 1000.
        ///     </code>
        /// </example>
        public static IQueryable<T> ApplySearchFilterFor<T, TValue>(this IQueryable<T> query, Expression<Func<T, TValue>> memberExpression, string filter)
            where T : class
        {
            return ApplySearchFilter(
                query,
                filter,
                LogicalRuleOperator.And, // Doesn't matter
                Guard.NotNull(memberExpression));
        }

        /// <inheritdoc cref="ApplySearchFilterFor{T, TValue}(IQueryable{T}, Expression{Func{T, TValue}}, string)"/>
        /// <summary>
        /// Applies a complex string-based DSL filter to given string members by combining 
        /// the predicates with <paramref name="logicalOperator"/>.
        /// </summary>
        /// <param name="logicalOperator">
        /// The logical operator to combine multiple <paramref name="memberExpressions"/> with.
        /// </param>
        /// <param name="memberExpressions">
        /// All member access expressions to build a combined lambda expression for.
        /// </param>
        public static IQueryable<T> ApplySearchFilter<T, TValue>(this IQueryable<T> query,
            string filter,
            LogicalRuleOperator logicalOperator,
            params Expression<Func<T, TValue>>[] memberExpressions)
            where T : class
        {
            Guard.NotNull(query);

            if (memberExpressions.Length == 0)
            {
                return query;
            }

            var filterExpressions = memberExpressions
                .Select(expression =>
                {
                    // TODO: (core) ErrorHandling and ModelState for ApplySearchTermFilter
                    if (FilterExpressionParser.TryParse(expression, filter, out var filterExpression))
                    {
                        return filterExpression;
                    }

                    return null;
                })
                .Where(x => x != null)
                .ToArray();

            if (filterExpressions.Length == 0)
            {
                return query;
            }
            else if (filterExpressions.Length == 1)
            {
                return query.Where(filterExpressions[0]).Cast<T>();
            }
            else
            {
                var compositeFilter = new FilterExpressionGroup(typeof(T), filterExpressions)
                {
                    LogicalOperator = logicalOperator
                };

                return query.Where(compositeFilter).Cast<T>();
            }
        }

        #endregion
    }
}
