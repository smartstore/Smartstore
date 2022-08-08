using Autofac;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Logging;

namespace Smartstore.Core.Search
{
    public abstract partial class SearchServiceBase
    {
        /// <summary>
        /// Flattens a list with filters including <see cref="ICombinedSearchFilter"/>.
        /// </summary>
        protected virtual void FlattenFilters(ICollection<ISearchFilter> filters, List<ISearchFilter> result)
        {
            foreach (var filter in filters)
            {
                if (filter is ICombinedSearchFilter combinedFilter)
                {
                    FlattenFilters(combinedFilter.Filters, result);
                }
                else
                {
                    result.Add(filter);
                }
            }
        }

        /// <summary>
        /// Searches for a filter including <see cref="ICombinedSearchFilter"/>.
        /// </summary>
        protected virtual ISearchFilter FindFilter(ICollection<ISearchFilter> filters, string fieldName)
        {
            if (fieldName.HasValue())
            {
                foreach (var filter in filters)
                {
                    if (filter is IAttributeSearchFilter attributeFilter && attributeFilter.FieldName == fieldName)
                    {
                        return attributeFilter;
                    }

                    if (filter is ICombinedSearchFilter combinedFilter)
                    {
                        var filter2 = FindFilter(combinedFilter.Filters, fieldName);
                        if (filter2 != null)
                        {
                            return filter2;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a list of entity identifiers from search filters excluding <see cref="IRangeSearchFilter"/>.
        /// </summary>
        protected virtual List<int> GetIdList(List<ISearchFilter> filters, string fieldName)
        {
            var result = new List<int>();

            foreach (IAttributeSearchFilter filter in filters)
            {
                if (!(filter is IRangeSearchFilter) && filter.FieldName == fieldName)
                {
                    result.Add((int)filter.Term);
                }
            }

            return result;
        }

        /// <summary>
        /// Helper to apply ordering to a query.
        /// </summary>
        protected virtual IOrderedQueryable<TEntity> OrderBy<TEntity, TKey>(
            ref bool ordered,
            IQueryable<TEntity> query,
            Expression<Func<TEntity, TKey>> keySelector,
            bool descending = false)
        {
            if (ordered)
            {
                if (descending)
                {
                    return ((IOrderedQueryable<TEntity>)query).ThenByDescending(keySelector);
                }

                return ((IOrderedQueryable<TEntity>)query).ThenBy(keySelector);
            }
            else
            {
                ordered = true;

                if (descending)
                {
                    return query.OrderByDescending(keySelector);
                }

                return query.OrderBy(keySelector);
            }
        }

        /// <summary>
        /// Notifies the admin that indexing is required to use the advanced search.
        /// </summary>
        protected virtual void IndexingRequiredNotification(ICommonServices services)
        {
            if (services.WorkContext.CurrentCustomer.IsAdmin() && services.Container.TryResolve<IUrlHelper>(out var urlHelper))
            {
                var indexingUrl = urlHelper.Action("Indexing", "MegaSearch", new { area = "Admin", scope = "Catalog" });
                var configureUrl = urlHelper.Action("ConfigureModule", "Module", new { area = "Admin", systemName = "Smartstore.MegaSearch" });
                var notification = services.Localization.GetResource("Search.IndexingRequiredNotification").FormatInvariant(indexingUrl, configureUrl);

                services.Notifier.Information(notification);
            }
        }
    }
}
