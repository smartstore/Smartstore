﻿using Microsoft.AspNetCore.Http;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Data;
using Smartstore.Net;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductCompareService(
        SmartDbContext db,
        IHttpContextAccessor httpContextAccessor,
        CatalogSettings catalogSettings,
        ICatalogSearchService catalogSearchService) : IProductCompareService
    {
        private readonly SmartDbContext _db = db;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly CatalogSettings _catalogSettings = catalogSettings;
        private readonly ICatalogSearchService _catalogSearchService = catalogSearchService;

        public virtual async Task<int> CountComparedProductsAsync()
        {
            var productIds = GetComparedProductsIds();
            if (productIds.Any())
            {
                var searchQuery = new CatalogSearchQuery()
                    .VisibleOnly()
                    .WithProductIds(productIds.ToArray())
                    .BuildHits(false);

                var result = await _catalogSearchService.SearchAsync(searchQuery);
                return result.TotalHitsCount;
            }

            return 0;
        }

        public virtual async Task<IList<Product>> GetCompareListAsync()
        {
            var productIds = GetComparedProductsIds();
            if (!productIds.Any())
            {
                return new List<Product>();
            }

            var query = _db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id));

            if (!_catalogSettings.IncludeFullDescriptionInCompareProducts)
            {
                query = query.SelectSummary();
            }

            var comparedProducts = await query
                .ApplyStandardFilter()
                .ToListAsync();

            return comparedProducts.OrderBySequence(productIds).ToList();
        }

        public virtual void ClearCompareList()
        {
            SetComparedProductsIds(null);
        }

        public virtual void RemoveFromList(int productId)
        {
            var productIds = GetComparedProductsIds();

            if (productIds.Contains(productId))
            {
                SetComparedProductsIds(productIds.Where(x => x != productId));
            }
        }

        public virtual void AddToList(int productId)
        {
            if (productId != 0)
            {
                var maxProducts = 4;
                var productIds = GetComparedProductsIds();
                var newProductIds = new List<int> { productId };

                newProductIds.AddRange(productIds
                    .Where(x => x != productId)
                    .Take(maxProducts - 1));

                SetComparedProductsIds(newProductIds);
            }
        }

        protected virtual IEnumerable<int> GetComparedProductsIds()
        {
            var request = _httpContextAccessor?.HttpContext?.Request;

            if (request != null && request.Cookies.TryGetValue(CookieNames.ComparedProducts, out var values) && values.HasValue())
            {
                return values.ToIntArray().Distinct();
            }

            return Enumerable.Empty<int>();
        }

        protected virtual void SetComparedProductsIds(IEnumerable<int> productIds)
        {
            var cookies = _httpContextAccessor.HttpContext?.Response?.Cookies;
            if (cookies == null)
            {
                return;
            }

            var cookieName = CookieNames.ComparedProducts;

            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(10.0),
                HttpOnly = true,
                IsEssential = true
            };

            cookies.Delete(cookieName, options);

            if (productIds?.Any() ?? false)
            {
                cookies.Append(cookieName, string.Join(',', productIds), options);
            }
        }
    }
}
