using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Core;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Search.Modelling
{
    /*
        TOKENS:
        ===============================
        q	-	Search term
        i	-	Page index
        o	-	Order by
        f   -   Forum
        c   -   Customer
        d   -   Date
    */

    public class ForumSearchQueryFactory : SearchQueryFactoryBase, IForumSearchQueryFactory
    {
        protected readonly ICommonServices _services;
        protected readonly IForumSearchQueryAliasMapper _forumSearchQueryAliasMapper;
        protected readonly IGenericAttributeService _genericAttributeService;
        protected readonly ForumSearchSettings _searchSettings;
        protected readonly ForumSettings _forumSettings;

        public ForumSearchQueryFactory(
            IHttpContextAccessor httpContextAccessor,
            ICommonServices services,
            IForumSearchQueryAliasMapper forumSearchQueryAliasMapper,
            IGenericAttributeService genericAttributeService,
            ForumSearchSettings searchSettings,
            ForumSettings forumSettings)
            : base(httpContextAccessor)
        {
            _services = services;
            _forumSearchQueryAliasMapper = forumSearchQueryAliasMapper;
            _genericAttributeService = genericAttributeService;
            _searchSettings = searchSettings;
            _forumSettings = forumSettings;
        }

        protected override string[] Tokens => new[] { "q", "i", "o", "f", "c", "d" };

        public ForumSearchQuery Current { get; private set; }

        public async Task<ForumSearchQuery> CreateFromQueryAsync()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx?.Request == null)
            {
                return null;
            }

            var area = ctx.Request.RouteValues.GetAreaName();
            var controller = ctx.Request.RouteValues.GetControllerName();
            var action = ctx.Request.RouteValues.GetActionName();
            var origin = "{0}{1}/{2}".FormatInvariant(area == null ? "" : area + "/", controller, action);
            var fields = new List<string> { "subject" };
            var term = GetValueFor<string>("q");
            var isInstantSearch = origin.EqualsNoCase("Boards/InstantSearch");

            fields.AddRange(_searchSettings.SearchFields);

            var query = new ForumSearchQuery(fields.ToArray(), term, _searchSettings.SearchMode)
                .OriginatesFrom(origin)
                .WithLanguage(_services.WorkContext.WorkingLanguage)
                .WithCurrency(_services.WorkContext.WorkingCurrency)
                .BuildFacetMap(!isInstantSearch);

            // Visibility.
            query.VisibleOnly(_services.WorkContext.CurrentCustomer, !_services.DbContext.QuerySettings.IgnoreAcl);

            // Store.
            if (!_services.DbContext.QuerySettings.IgnoreMultiStore)
            {
                query.HasStoreId(_services.StoreContext.CurrentStore.Id);
            }

            // Instant-Search never uses these filter parameters.
            if (!isInstantSearch)
            {
                ConvertPagingSorting(query, origin);
                ConvertForum(query, origin);
                ConvertCustomer(query, origin);
                ConvertDate(query, origin);
            }

            await OnConvertedAsync(query, origin);

            Current = query;
            return query;
        }

        protected virtual void ConvertPagingSorting(ForumSearchQuery query, string origin)
        {
            var index = Math.Max(1, GetValueFor<int?>("i") ?? 1);
            var size = GetPageSize(query, origin);
            query.Slice((index - 1) * size, size);

            if (_forumSettings.AllowSorting)
            {
                var orderBy = GetValueFor<ForumTopicSorting?>("o");
                if (orderBy == null || orderBy == ForumTopicSorting.Initial)
                {
                    orderBy = _searchSettings.DefaultSortOrder;
                }

                query.SortBy(orderBy.Value);
                query.CustomData["CurrentSortOrder"] = orderBy.Value;
            }
        }

        protected virtual int GetPageSize(ForumSearchQuery query, string origin)
        {
            return _forumSettings.SearchResultsPageSize;
        }

        private void AddFacet(
            ForumSearchQuery query,
            FacetGroupKind kind,
            bool isMultiSelect,
            FacetSorting sorting,
            Action<FacetDescriptor> addValues)
        {
            string fieldName;
            int displayOrder;

            switch (kind)
            {
                case FacetGroupKind.Forum:
                    fieldName = "forumid";
                    displayOrder = _searchSettings.ForumDisplayOrder;
                    break;
                case FacetGroupKind.Customer:
                    fieldName = "customerid";
                    displayOrder = _searchSettings.CustomerDisplayOrder;
                    break;
                case FacetGroupKind.Date:
                    fieldName = "createdon";
                    displayOrder = _searchSettings.DateDisplayOrder;
                    break;
                default:
                    throw new SmartException($"Unknown field name for facet group '{kind}'");
            }

            var descriptor = new FacetDescriptor(fieldName)
            {
                Label = _services.Localization.GetResource(FacetUtility.GetLabelResourceKey(kind) ?? kind.ToString()),
                IsMultiSelect = isMultiSelect,
                DisplayOrder = displayOrder,
                OrderBy = sorting,
                MinHitCount = _searchSettings.FilterMinHitCount,
                MaxChoicesCount = _searchSettings.FilterMaxChoicesCount
            };

            addValues(descriptor);
            query.WithFacet(descriptor);
        }

        protected virtual void ConvertForum(ForumSearchQuery query, string origin)
        {
            var alias = _forumSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.Forum, query.LanguageId ?? 0);

            if (TryGetValueFor(alias ?? "f", out List<int> ids) && ids != null && ids.Any())
            {
                query.WithForumIds(ids.ToArray());
            }

            AddFacet(query, FacetGroupKind.Forum, true, FacetSorting.HitsDesc, descriptor =>
            {
                if (ids != null)
                {
                    foreach (var id in ids)
                    {
                        descriptor.AddValue(new FacetValue(id, IndexTypeCode.Int32)
                        {
                            IsSelected = true
                        });
                    }
                }
            });
        }

        protected virtual void ConvertCustomer(ForumSearchQuery query, string origin)
        {
            var alias = _forumSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.Customer, query.LanguageId ?? 0);

            if (TryGetValueFor(alias ?? "c", out List<int> ids) && ids != null && ids.Any())
            {
                query.WithCustomerIds(ids.ToArray());
            }

            AddFacet(query, FacetGroupKind.Customer, true, FacetSorting.HitsDesc, descriptor =>
            {
                if (ids != null)
                {
                    foreach (var id in ids)
                    {
                        descriptor.AddValue(new FacetValue(id, IndexTypeCode.Int32)
                        {
                            IsSelected = true
                        });
                    }
                }
            });
        }

        protected virtual void ConvertDate(ForumSearchQuery query, string origin)
        {
            DateTime? fromUtc = null;
            DateTime? toUtc = null;
            var alias = _forumSearchQueryAliasMapper.GetCommonFacetAliasByGroupKind(FacetGroupKind.Date, query.LanguageId ?? 0);

            if (TryGetValueFor(alias ?? "d", out string date) && TryParseRange(date, out fromUtc, out toUtc))
            {
                if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
                {
                    var tmp = fromUtc;
                    fromUtc = toUtc;
                    toUtc = tmp;
                }

                if (fromUtc.HasValue || toUtc.HasValue)
                {
                    query.CreatedBetween(fromUtc, toUtc);
                }
            }

            AddFacet(query, FacetGroupKind.Date, false, FacetSorting.DisplayOrder, descriptor =>
            {
                AddDates(descriptor, fromUtc, toUtc);
            });
        }

        protected virtual void AddDates(FacetDescriptor descriptor, DateTime? selectedFrom, DateTime? selectedTo)
        {
            var customer = _services.WorkContext.CurrentCustomer;
            var count = 0;
            var utcNow = DateTime.UtcNow;
            utcNow = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0);

            foreach (ForumDateFilter filter in Enum.GetValues(typeof(ForumDateFilter)))
            {
                var dt = utcNow.AddDays(-((int)filter));

                if (filter == ForumDateFilter.LastVisit)
                {
                    var lastVisit = customer.LastForumVisit;
                    if (!lastVisit.HasValue)
                    {
                        continue;
                    }

                    dt = lastVisit.Value;
                }

                var value = selectedTo.HasValue
                    ? new FacetValue(null, dt, IndexTypeCode.DateTime, false, true)
                    : new FacetValue(dt, null, IndexTypeCode.DateTime, true, false);

                value.DisplayOrder = ++count;
                // TODO: (mg) (core) update forum string resource key.
                value.Label = _services.Localization.GetResource("Enums.SmartStore.Core.Domain.Forums.ForumDateFilter." + filter.ToString());

                if (selectedFrom.HasValue)
                {
                    value.IsSelected = dt == selectedFrom.Value;
                }
                else if (selectedTo.HasValue)
                {
                    value.IsSelected = dt == selectedTo.Value;
                }

                descriptor.AddValue(value);
            }
        }

        protected virtual Task OnConvertedAsync(ForumSearchQuery query, string origin)
        {
            return Task.CompletedTask;
        }
    }
}
