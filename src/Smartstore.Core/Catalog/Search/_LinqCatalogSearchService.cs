using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic;
using MimeKit.Cryptography;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Diagnostics;
using Smartstore.Events;

namespace Smartstore.Core.Catalog.Search
{
    public partial class LinqCatalogSearchService : SearchServiceBase, ICatalogSearchService
    {
        private static readonly int[] _priceThresholds = new int[] { 10, 25, 50, 100, 250, 500, 1000 };

        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ICategoryService _categoryService;

        public LinqCatalogSearchService(
            SmartDbContext db,
            ICommonServices services,
            ICategoryService categoryService)
        {
            _db = db;
            _services = services;
            _categoryService = categoryService;
        }

        public IQueryable<Product> PrepareQuery(CatalogSearchQuery searchQuery, IQueryable<Product> baseQuery = null)
        {
            throw new NotImplementedException();
        }

        public Task<CatalogSearchResult> SearchAsync(CatalogSearchQuery searchQuery, bool direct = false)
        {
            throw new NotImplementedException();
        }


        #region Utilities

        protected virtual async Task<IDictionary<string, FacetGroup>> GetFacetsAsync(CatalogSearchQuery searchQuery, int totalHits)
        {
            var result = new Dictionary<string, FacetGroup>();
            var storeId = searchQuery.StoreId ?? _services.StoreContext.CurrentStore.Id;
            var languageId = searchQuery.LanguageId ?? _services.WorkContext.WorkingLanguage.Id;

            foreach (var key in searchQuery.FacetDescriptors.Keys)
            {
                var descriptor = searchQuery.FacetDescriptors[key];
                var facets = new List<Facet>();
                var kind = FacetGroup.GetKindByKey(CatalogSearchService.Scope, key);

                switch (kind)
                {
                    case FacetGroupKind.Category:
                    case FacetGroupKind.Brand:
                    case FacetGroupKind.DeliveryTime:
                    case FacetGroupKind.Rating:
                    case FacetGroupKind.Price:
                        if (totalHits == 0 && !descriptor.Values.Any(x => x.IsSelected))
                        {
                            continue;
                        }
                        break;
                }

                if (kind == FacetGroupKind.Category)
                {
                    var names = await GetLocalizedNames(nameof(Category), languageId);
                    var categoryTree = await _categoryService.GetCategoryTreeAsync(0, false, storeId);
                    var categories = categoryTree.Flatten(false);

                    if (descriptor.MaxChoicesCount > 0)
                    {
                        categories = categories.Take(descriptor.MaxChoicesCount);
                    }

                    foreach (var category in categories)
                    {
                        names.TryGetValue(category.Id, out var label);

                        facets.Add(new Facet(new FacetValue(category.Id, IndexTypeCode.Int32)
                        {
                            IsSelected = descriptor.Values.Any(x => x.IsSelected && x.Value.Equals(category.Id)),
                            Label = label.HasValue() ? label : category.Name,
                            DisplayOrder = category.DisplayOrder
                        }));
                    }
                }
                else if (kind == FacetGroupKind.Brand)
                {
                    var names = await GetLocalizedNames(nameof(Manufacturer), languageId);
                    var customerRolesIds = _services.WorkContext.CurrentCustomer.GetRoleIds();
                    var manufacturersQuery = _db.Manufacturers
                        .AsNoTracking()
                        .ApplyStandardFilter(false, customerRolesIds, storeId);
                    
                    // "ApplyPaging" helper?
                    var manufacturers = descriptor.MaxChoicesCount > 0
                        ? await manufacturersQuery.Take(descriptor.MaxChoicesCount).ToListAsync()
                        : await manufacturersQuery.ToListAsync();

                    foreach (var manu in manufacturers)
                    {
                        names.TryGetValue(manu.Id, out var label);

                        facets.Add(new Facet(new FacetValue(manu.Id, IndexTypeCode.Int32)
                        {
                            IsSelected = descriptor.Values.Any(x => x.IsSelected && x.Value.Equals(manu.Id)),
                            Label = label.HasValue() ? label : manu.Name,
                            DisplayOrder = manu.DisplayOrder
                        }));
                    }
                }
                else if (kind == FacetGroupKind.DeliveryTime)
                {
                    var names = await GetLocalizedNames(nameof(DeliveryTime), languageId);
                    var deliveryTimesQuery = _db.DeliveryTimes
                        .AsNoTracking()
                        .OrderBy(x => x.DisplayOrder);

                    var deliveryTimes = descriptor.MaxChoicesCount > 0
                        ? await deliveryTimesQuery.Take(descriptor.MaxChoicesCount).ToListAsync()
                        : await deliveryTimesQuery.ToListAsync();


                }

            }

            return result;
        }

        private async Task<Dictionary<int, string>> GetLocalizedNames(string entityName, int languageId, string key = "Name")
        {
            var values = await _db.LocalizedProperties
                .Where(x => x.LocaleKeyGroup == entityName && x.LocaleKey == key && x.LanguageId == languageId)
                .Select(x => new { x.EntityId, x.LocaleValue })
                .ToListAsync();

            var result = values.ToDictionarySafe(x => x.EntityId, x => x.LocaleValue);
            return result;
        }

        #endregion
    }
}
