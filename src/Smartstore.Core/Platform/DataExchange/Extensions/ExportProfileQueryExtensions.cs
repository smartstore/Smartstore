using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Core.DataExchange.Export
{
    public static partial class ExportProfileQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter for an export profile query.
        /// Includes <see cref="ExportProfile.Deployments"/> and sorts by 
        /// <see cref="ExportProfile.IsSystemProfile"/>, then by <see cref="ExportProfile.Name"/>.
        /// </summary>
        /// <param name="query">Export profile query.</param>
        /// <param name="enabled">A value indicating whether to include enabled profiles. <c>null</c> to ignore.</param>
        /// <returns>Export profile query.</returns>
        public static IOrderedQueryable<ExportProfile> ApplyStandardFilter(this IQueryable<ExportProfile> query, bool? enabled = null)
        {
            Guard.NotNull(query, nameof(query));

            query = query.Include(x => x.Deployments);

            if (enabled.HasValue)
            {
                query = query.Where(x => x.Enabled == enabled.Value);
            }

            return query
                .OrderBy(x => x.IsSystemProfile)
                .ThenBy(x => x.Name);
        }

        /// <summary>
        /// Applies a filter for system export profiles.
        /// </summary>
        /// <param name="query">Export profile query.</param>
        /// <param name="providerSystemName">Name of the export provider.</param>
        /// <returns>Export profile query.</returns>
        public static IQueryable<ExportProfile> ApplySystemProfileFilter(this IQueryable<ExportProfile> query, string providerSystemName)
        {
            Guard.NotNull(query, nameof(query));

            if (providerSystemName.IsEmpty())
            {
                return query;
            }

            return query.Where(x => x.IsSystemProfile && x.ProviderSystemName == providerSystemName);
        }
    }
}
