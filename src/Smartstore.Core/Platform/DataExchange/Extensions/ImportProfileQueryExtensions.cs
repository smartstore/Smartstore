namespace Smartstore.Core.DataExchange.Import
{
    public static partial class ImportProfileQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter for an import profile query.
        /// Includes <see cref="ImportProfile.Task"/> and sorts by 
        /// <see cref="ImportProfile.EntityTypeId"/>, then by <see cref="ImportProfile.Name"/>.
        /// </summary>
        /// <param name="query">Import profile query.</param>
        /// <param name="enabled">A value indicating whether to include enabled profiles. <c>null</c> to ignore.</param>
        /// <returns>Import profile query.</returns>
        public static IOrderedQueryable<ImportProfile> ApplyStandardFilter(this IQueryable<ImportProfile> query, bool? enabled = null)
        {
            Guard.NotNull(query, nameof(query));

            query = query.Include(x => x.Task);

            if (enabled.HasValue)
            {
                query = query.Where(x => x.Enabled == enabled.Value);
            }

            return query
                .OrderBy(x => x.EntityTypeId)
                .ThenBy(x => x.Name);
        }
    }
}
