using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Identity;
using Smartstore.Core.Search;
using Smartstore.Forums.Search.Modelling;

namespace Smartstore.Forums.Search
{
    [ModelBinder(typeof(ForumSearchQueryModelBinder))]
    [ValidateNever]
    public partial class ForumSearchQuery : SearchQuery<ForumSearchQuery>, ICloneable<ForumSearchQuery>
    {
        private readonly static Func<DbSet<ForumPost>, int[], Task<List<ForumPost>>> _defaultHitsFactory = async (dbSet, ids) =>
        {
            var items = await dbSet.AsNoTracking()
                .IncludeTopic()
                .IncludeCustomer()
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            return items.OrderBySequence(ids).ToList();
        };

        private readonly static Func<DbSet<ForumPost>, int[], Task<List<ForumPost>>> _defaultInstantSearchHitsFactory = async (dbSet, ids) =>
        {
            var items = await dbSet.AsNoTracking()
                .IncludeTopic()
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            return items.OrderBySequence(ids).ToList();
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ForumSearchQuery"/> class without a search term being set.
        /// </summary>
        public ForumSearchQuery()
            : base(null, null)
        {
        }

        public ForumSearchQuery(string field, string term, SearchMode mode = SearchMode.Contains, bool escape = true, bool isFuzzySearch = false)
            : base(field.HasValue() ? new[] { field } : null, term, mode, escape, isFuzzySearch)
        {
        }

        public ForumSearchQuery(string[] fields, string term, SearchMode mode = SearchMode.Contains, bool escape = true, bool isFuzzySearch = false)
            : base(fields, term, mode, escape, isFuzzySearch)
        {
        }

        public ForumSearchQuery Clone()
            => (ForumSearchQuery)MemberwiseClone();

        object ICloneable.Clone()
            => MemberwiseClone();

        // Using Func<> properties in bindable models significantly reduces response time
        // due to a "bug" in the MVC model binding/validation system: https://github.com/dotnet/aspnetcore/issues/27709
        public Func<DbSet<ForumPost>, int[], Task<List<ForumPost>>> GetHitsFactory()
        {
            return Origin.EqualsNoCase("Boards/InstantSearch")
                ? _defaultInstantSearchHitsFactory
                : _defaultHitsFactory;
        }

        #region Fluent builder

        public ForumSearchQuery SortBy(ForumTopicSorting sort)
        {
            switch (sort)
            {
                case ForumTopicSorting.SubjectAsc:
                case ForumTopicSorting.SubjectDesc:
                    return SortBy(SearchSort.ByStringField("subject", sort == ForumTopicSorting.SubjectDesc));

                case ForumTopicSorting.UserNameAsc:
                case ForumTopicSorting.UserNameDesc:
                    return SortBy(SearchSort.ByStringField("username", sort == ForumTopicSorting.UserNameDesc));

                case ForumTopicSorting.CreatedOnAsc:
                case ForumTopicSorting.CreatedOnDesc:
                    return SortBy(SearchSort.ByDateTimeField("createdon", sort == ForumTopicSorting.CreatedOnDesc));

                case ForumTopicSorting.PostsAsc:
                case ForumTopicSorting.PostsDesc:
                    return SortBy(SearchSort.ByIntField("numposts", sort == ForumTopicSorting.PostsDesc));

                case ForumTopicSorting.Relevance:
                    return SortBy(SearchSort.ByRelevance());

                default:
                    return this;
            }
        }

        public ForumSearchQuery VisibleOnly(Customer customer, bool includeCustomerRoles)
        {
            if (customer != null)
            {
                if (!customer.IsForumModerator())
                {
                    // See also LinqForumSearchService.
                    var publishedCombined = SearchFilter.Combined(
                        SearchFilter.ByField("published", true).ExactMatch().NotAnalyzed(),
                        SearchFilter.ByField("customerid", customer.Id).ExactMatch().NotAnalyzed());

                    WithFilter(publishedCombined);
                }

                if (includeCustomerRoles)
                {
                    var allowedRoleIds = customer.GetRoleIds();
                    if (allowedRoleIds != null && allowedRoleIds.Length > 0)
                    {
                        var roleIds = allowedRoleIds.Where(x => x != 0).Distinct().ToList();
                        if (roleIds.Any())
                        {
                            roleIds.Insert(0, 0);
                            WithFilter(SearchFilter.Combined(roleIds.Select(x => SearchFilter.ByField("roleid", x).ExactMatch().NotAnalyzed()).ToArray()));
                        }
                    }
                }
            }

            return this;
        }

        public ForumSearchQuery PublishedOnly(bool value)
        {
            return WithFilter(SearchFilter.ByField("published", value).Mandatory().ExactMatch().NotAnalyzed());
        }

        public override ForumSearchQuery HasStoreId(int id)
        {
            base.HasStoreId(id);

            if (id == 0)
            {
                WithFilter(SearchFilter.ByField("storeid", 0).ExactMatch().NotAnalyzed());
            }
            else
            {
                WithFilter(SearchFilter.Combined(
                    SearchFilter.ByField("storeid", 0).ExactMatch().NotAnalyzed(),
                    SearchFilter.ByField("storeid", id).ExactMatch().NotAnalyzed())
                );
            }

            return this;
        }

        public ForumSearchQuery WithForumIds(params int[] ids)
        {
            if (ids.Length == 0)
            {
                return this;
            }

            return WithFilter(SearchFilter.Combined(ids.Select(x => SearchFilter.ByField("forumid", x).ExactMatch().NotAnalyzed()).ToArray()));
        }

        public ForumSearchQuery WithCustomerIds(params int[] ids)
        {
            if (ids.Length == 0)
            {
                return this;
            }

            return WithFilter(SearchFilter.Combined(ids.Select(x => SearchFilter.ByField("customerid", x).ExactMatch().NotAnalyzed()).ToArray()));
        }

        public ForumSearchQuery CreatedBetween(DateTime? fromUtc, DateTime? toUtc)
        {
            if (fromUtc == null && toUtc == null)
            {
                return this;
            }

            return WithFilter(SearchFilter.ByRange("createdon", fromUtc, toUtc, fromUtc.HasValue, toUtc.HasValue).Mandatory().ExactMatch().NotAnalyzed());
        }

        #endregion
    }
}
