namespace Smartstore.Core.Rules
{
    public static partial class RuleQueryExtensions
    {
        /// <summary>
        /// Applies ruleset standard filter and sorts by <see cref="RuleSetEntity.IsActive"/> DESC, then by <see cref="RuleSetEntity.Scope"/>.
        /// </summary>
        public static IOrderedQueryable<RuleSetEntity> ApplyStandardFilter(this IQueryable<RuleSetEntity> query,
            RuleScope? scope = null,
            bool includeSubGroups = false,
            bool includeHidden = false)
        {
            Guard.NotNull(query);

            query = query.Where(x => x.Scope < RuleScope.ProductAttribute);

            if (!includeHidden)
            {
                query = query.Where(x => x.IsActive);
            }

            if (scope != null)
            {
                query = query.Where(x => x.Scope == scope.Value);
            }

            if (!includeSubGroups)
            {
                query = query.Where(x => !x.IsSubGroup);
            }

            return query
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Scope);
        }
    }
}
