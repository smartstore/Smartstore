using Microsoft.AspNetCore.Http;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Data;
using Smartstore.Net;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductCompareService : IProductCompareService
    {
        private readonly SmartDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICatalogSearchService _catalogSearchService;

        public ProductCompareService(
            SmartDbContext db,
            IHttpContextAccessor httpContextAccessor,
            ICatalogSearchService catalogSearchService)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _catalogSearchService = catalogSearchService;
        }

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

            var comparedProducts = await _db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
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
