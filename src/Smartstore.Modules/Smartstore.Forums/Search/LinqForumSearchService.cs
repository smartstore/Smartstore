using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Search
{
    public partial class LinqForumSearchService : SearchServiceBase, IForumSearchService
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly CustomerSettings _customerSettings;

        public LinqForumSearchService(
            SmartDbContext db, 
            ICommonServices services,
            CustomerSettings customerSettings)
        {
            _db = db;
            _services = services;
            _customerSettings = customerSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public IQueryable<ForumPost> PrepareQuery(ForumSearchQuery searchQuery, IQueryable<ForumPost> baseQuery = null)
        {
            return GetPostQuery(searchQuery, baseQuery);
        }

        public async Task<ForumSearchResult> SearchAsync(ForumSearchQuery searchQuery, bool direct = false)
        {
            await _services.EventPublisher.PublishAsync(new ForumSearchingEvent(searchQuery));

            var totalHits = 0;
            int[] hitsEntityIds = null;
            IDictionary<string, FacetGroup> facets = null;

            if (searchQuery.Take > 0)
            {
                var query = GetPostQuery(searchQuery, null);

                totalHits = await query.CountAsync();

                // Fix paging boundaries.
                if (searchQuery.Skip > 0 && searchQuery.Skip >= totalHits)
                {
                    searchQuery.Slice((totalHits / searchQuery.Take) * searchQuery.Take, searchQuery.Take);
                }

                if (searchQuery.ResultFlags.HasFlag(SearchResultFlags.WithHits))
                {
                    var skip = searchQuery.PageIndex * searchQuery.Take;

                    query = query
                        .Skip(skip)
                        .Take(searchQuery.Take);

                    hitsEntityIds = query.Select(x => x.Id).ToArray();
                }

                if (searchQuery.ResultFlags.HasFlag(SearchResultFlags.WithFacets) && searchQuery.FacetDescriptors.Any())
                {
                    facets = await GetFacetsAsync(searchQuery, totalHits);
                }
            }

            var result = new ForumSearchResult(
                null,
                searchQuery,
                _db.ForumPosts(),
                totalHits,
                hitsEntityIds,
                null,
                facets);

            var searchedEvent = new ForumSearchedEvent(searchQuery, result);
            await _services.EventPublisher.PublishAsync(searchedEvent);

            return searchedEvent.Result;
        }

        protected virtual IQueryable<ForumPost> GetPostQuery(ForumSearchQuery searchQuery, IQueryable<ForumPost> baseQuery)
        {
            var ctx = new QueryBuilderContext
            {
                SearchQuery = searchQuery
            };

            var query = baseQuery ?? _db.ForumPosts().Include(x => x.ForumTopic).AsNoTracking();
            
            query = ApplySearchTerm(ctx, query);
            query = FlattenFilters(ctx, query);

            foreach (IAttributeSearchFilter filter in ctx.Filters)
            {
                if (filter is IRangeSearchFilter rf)
                {
                    query = ApplyRangeFilter(ctx, query, rf);
                }
                else
                {
                    // Filters that can have both range and comparison values.
                    if (filter.FieldName == "forumid")
                    {
                        query = query.Where(x => x.ForumTopic.ForumId == (int)filter.Term);
                    }
                    else if (filter.FieldName == "customerid")
                    {
                        query = query.Where(x => x.CustomerId == (int)filter.Term);
                    }
                    else if (filter.FieldName == "published")
                    {
                        query = query.Where(x => x.Published == (bool)filter.Term);
                    }
                    else if (filter.FieldName == "storeid")
                    {
                        query = query.ApplyStoreFilter((int)filter.Term);
                    }
                }
            }

            query = ApplyAclFilter(ctx, query);

            if (ctx.IsGroupingRequired)
            {
                // Distinct is very slow if there are many forum posts.
                query = query.Distinct();
            }

            query = ApplyOrdering(ctx, query);

            return query;
        }

        protected virtual async Task<IDictionary<string, FacetGroup>> GetFacetsAsync(ForumSearchQuery searchQuery, int totalHits)
        {
            var result = new Dictionary<string, FacetGroup>();
            var storeId = searchQuery.StoreId ?? _services.StoreContext.CurrentStore.Id;
            var languageId = searchQuery.LanguageId ?? _services.WorkContext.WorkingLanguage.Id;

            foreach (var key in searchQuery.FacetDescriptors.Keys)
            {
                var descriptor = searchQuery.FacetDescriptors[key];
                var facets = new List<Facet>();
                var kind = FacetGroup.GetKindByKey(ForumSearchService.Scope, key);

                if (kind == FacetGroupKind.Forum)
                {
                    var enoughFacets = false;
                    var customer = _services.WorkContext.CurrentCustomer;

                    var groups = await _db.ForumGroups()
                        .Include(x => x.Forums)
                        .AsNoTracking()
                        .ApplyStoreFilter(storeId)
                        .ApplyAclFilter(customer)
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync();

                    foreach (var group in groups)
                    {
                        foreach (var forum in group.Forums)
                        {
                            facets.Add(new Facet(new FacetValue(forum.Id, IndexTypeCode.Int32)
                            {
                                IsSelected = descriptor.Values.Any(x => x.IsSelected && x.Value.Equals(forum.Id)),
                                Label = forum.GetLocalized(x => x.Name, languageId),
                                DisplayOrder = forum.DisplayOrder
                            }));

                            if (descriptor.MaxChoicesCount > 0 && facets.Count >= descriptor.MaxChoicesCount)
                            {
                                enoughFacets = true;
                                break;
                            }
                        }

                        if (enoughFacets)
                        {
                            break;
                        }
                    }
                }
                else if (kind == FacetGroupKind.Customer)
                {
                    // Get customers with most posts.
                    // Limit the result. Do not allow to get all customers.
                    var maxChoices = descriptor.MaxChoicesCount > 0 ? descriptor.MaxChoicesCount : 20;
                    var take = maxChoices * 3;

                    var forumPostQuery = _db.ForumPosts()
                        .AsNoTracking()
                        .ApplyStoreFilter(storeId);

                    forumPostQuery = forumPostQuery.Where(x => 
                        x.Customer.CustomerRoleMappings.FirstOrDefault(y => y.CustomerRole.SystemName == SystemCustomerRoleNames.Guests) == null && x.Customer.Active && !x.Customer.IsSystemAccount);

                    var groupQuery =
                        from fp in forumPostQuery
                        group fp by fp.CustomerId into grp
                        select new
                        {
                            Count = grp.Count(),
                            CustomerId = grp.Key
                        };

                    if (descriptor.MinHitCount > 1)
                    {
                        groupQuery = groupQuery.Where(x => x.Count >= descriptor.MinHitCount);
                    }

                    var customerIdQuery = groupQuery
                        .OrderByDescending(x => x.Count)
                        .Select(x => x.CustomerId);

                    var customers = await _db.Customers
                        .Include(x => x.BillingAddress)
                        .Include(x => x.ShippingAddress)
                        .Include(x => x.Addresses)
                        .AsNoTracking()
                        .Where(x => customerIdQuery.Contains(x.Id))
                        .OrderBy(x => x.Id)
                        .Take(take)
                        .ToListAsync();

                    foreach (var customer in customers)
                    {
                        var name = customer.FormatUserName(_customerSettings, T, true);
                        if (name.HasValue())
                        {
                            facets.Add(new Facet(new FacetValue(customer.Id, IndexTypeCode.Int32)
                            {
                                IsSelected = descriptor.Values.Any(x => x.IsSelected && x.Value.Equals(customer.Id)),
                                Label = name,
                                DisplayOrder = 0
                            }));
                            if (facets.Count >= maxChoices)
                            {
                                break;
                            }
                        }
                    }
                }
                else if (kind == FacetGroupKind.Date)
                {
                    foreach (var value in descriptor.Values)
                    {
                        facets.Add(new Facet(value));
                    }
                }

                if (facets.Any(x => x.Published))
                {
                    //facets.Each(x => $"{key} {x.Value.ToString()}".Dump());

                    var group = new FacetGroup(
                        ForumSearchService.Scope,
                        key,
                        descriptor.Label,
                        descriptor.IsMultiSelect,
                        false,
                        descriptor.DisplayOrder,
                        facets.OrderBy(descriptor))
                    {
                        IsScrollable = facets.Count > 14
                    };

                    result.Add(key, group);
                }
            }

            return result;
        }

        private IQueryable<ForumPost> ApplySearchTerm(QueryBuilderContext ctx, IQueryable<ForumPost> query)
        {
            var t = ctx.SearchQuery.Term;
            var fields = ctx.SearchQuery.Fields;
            var cnf = _customerSettings.CustomerNameFormat;

            if (t.HasValue() && fields != null && fields.Length != 0 && fields.Any(x => x.HasValue()))
            {
                ctx.IsGroupingRequired = true;

                if (ctx.SearchQuery.Mode == SearchMode.StartsWith)
                {
                    query = query.Where(x =>
                        (fields.Contains("subject") && x.ForumTopic.Subject.StartsWith(t)) ||
                        (fields.Contains("text") && x.Text.StartsWith(t)) ||
                        (fields.Contains("username") && (
                            cnf == CustomerNameFormat.ShowEmails ? x.Customer.Email.StartsWith(t) :
                            cnf == CustomerNameFormat.ShowUsernames ? x.Customer.Username.StartsWith(t) :
                            cnf == CustomerNameFormat.ShowFirstName ? x.Customer.FirstName.StartsWith(t) :
                            x.Customer.FullName.StartsWith(t))
                        ));
                }
                else
                {
                    query = query.Where(x =>
                        (fields.Contains("subject") && x.ForumTopic.Subject.Contains(t)) ||
                        (fields.Contains("text") && x.Text.Contains(t)) ||
                        (fields.Contains("username") && (
                            cnf == CustomerNameFormat.ShowEmails ? x.Customer.Email.Contains(t) :
                            cnf == CustomerNameFormat.ShowUsernames ? x.Customer.Username.Contains(t) :
                            cnf == CustomerNameFormat.ShowFirstName ? x.Customer.FirstName.Contains(t) :
                            x.Customer.FullName.Contains(t))
                        ));
                }
            }

            return query;
        }

        private IQueryable<ForumPost> FlattenFilters(QueryBuilderContext ctx, IQueryable<ForumPost> query)
        {
            var customer = _services.WorkContext.CurrentCustomer;

            foreach (var filter in ctx.SearchQuery.Filters)
            {
                if (filter is ICombinedSearchFilter combinedFilter)
                {
                    // Find VisibleOnly combined filter and process it separately.
                    var cf = combinedFilter.Filters.OfType<IAttributeSearchFilter>().ToArray();
                    if (cf.Length == 2 && cf[0].FieldName == "published" && true == (bool)cf[0].Term && cf[1].FieldName == "customerid")
                    {
                        if (!customer.IsForumModerator())
                        {
                            query = query.Where(x => x.ForumTopic.Published && (x.Published || x.CustomerId == customer.Id));
                        }
                    }
                    else
                    {
                        FlattenFilters(combinedFilter.Filters, ctx.Filters);
                    }
                }
                else
                {
                    ctx.Filters.Add(filter);
                }
            }

            return query;
        }

        private static IQueryable<ForumPost> ApplyRangeFilter(QueryBuilderContext ctx, IQueryable<ForumPost> query, IRangeSearchFilter rf)
        {
            if (rf.FieldName == "id")
            {
                var lower = rf.Term as int?;
                var upper = rf.UpperTerm as int?;

                if (lower.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => x.Id >= lower.Value);
                    else
                        query = query.Where(x => x.Id > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (rf.IncludesUpper)
                        query = query.Where(x => x.Id <= upper.Value);
                    else
                        query = query.Where(x => x.Id < upper.Value);
                }
            }
            else if (rf.FieldName == "createdon")
            {
                var lower = rf.Term as DateTime?;
                var upper = rf.UpperTerm as DateTime?;

                if (lower.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => x.CreatedOnUtc >= lower.Value);
                    else
                        query = query.Where(x => x.CreatedOnUtc > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => x.CreatedOnUtc <= upper.Value);
                    else
                        query = query.Where(x => x.CreatedOnUtc < upper.Value);
                }
            }

            return query;
        }

        private IQueryable<ForumPost> ApplyAclFilter(QueryBuilderContext ctx, IQueryable<ForumPost> query)
        {
            if (!_db.QuerySettings.IgnoreAcl)
            {
                var roleIds = GetIdList(ctx.Filters, "roleid");
                if (roleIds.Any())
                {
                    ctx.IsGroupingRequired = true;

                    // Do not use ApplyAclFilter extension method to avoid multiple grouping.
                    query =
                        from fp in query
                        join ft in _db.ForumTopics().AsNoTracking() on fp.TopicId equals ft.Id
                        join ff in _db.Forums().AsNoTracking() on ft.ForumId equals ff.Id
                        join fg in _db.ForumGroups().AsNoTracking() on ff.ForumGroupId equals fg.Id
                        join a in _db.AclRecords.AsNoTracking() on new { a1 = fg.Id, a2 = "ForumGroup" } equals new { a1 = a.EntityId, a2 = a.EntityName } into fg_acl
                        from a in fg_acl.DefaultIfEmpty()
                        where !fg.SubjectToAcl || roleIds.Contains(a.CustomerRoleId)
                        select fp;
                }
            }

            return query;
        }

        private IQueryable<ForumPost> ApplyOrdering(QueryBuilderContext ctx, IQueryable<ForumPost> query)
        {
            var ordered = false;

            foreach (var sort in ctx.SearchQuery.Sorting)
            {
                if (sort.FieldName == "subject")
                {
                    query = OrderBy(ref ordered, query, x => x.ForumTopic.Subject, sort.Descending);
                }
                else if (sort.FieldName == "username")
                {
                    query = _customerSettings.CustomerNameFormat switch
                    {
                        CustomerNameFormat.ShowEmails => OrderBy(ref ordered, query, x => x.Customer.Email, sort.Descending),
                        CustomerNameFormat.ShowUsernames => OrderBy(ref ordered, query, x => x.Customer.Username, sort.Descending),
                        CustomerNameFormat.ShowFirstName => OrderBy(ref ordered, query, x => x.Customer.FirstName, sort.Descending),
                        _ => OrderBy(ref ordered, query, x => x.Customer.FullName, sort.Descending),
                    };
                }
                else if (sort.FieldName == "createdon")
                {
                    // We want to sort by ForumPost.CreatedOnUtc, not ForumTopic.CreatedOnUtc.
                    query = OrderBy(ref ordered, query, x => x.ForumTopic.LastPostTime, sort.Descending);
                }
                else if (sort.FieldName == "numposts")
                {
                    query = OrderBy(ref ordered, query, x => x.ForumTopic.NumPosts, sort.Descending);
                }
            }

            if (!ordered)
            {
                query = query
                    .OrderByDescending(x => x.ForumTopic.TopicTypeId)
                    .ThenByDescending(x => x.ForumTopic.LastPostTime)
                    .ThenByDescending(x => x.TopicId);
            }

            return query;
        }

        protected class QueryBuilderContext
        {
            public ForumSearchQuery SearchQuery { get; init; }
            public List<ISearchFilter> Filters { get; init; } = new();
            public DateTime Now { get; init; } = DateTime.UtcNow;
            public bool IsGroupingRequired { get; set; }
        }
    }
}
