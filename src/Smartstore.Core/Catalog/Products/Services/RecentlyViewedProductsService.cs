
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Net;

namespace Smartstore.Core.Catalog.Products
{
    public partial class RecentlyViewedProductsService : IRecentlyViewedProductsService
    {
        private readonly SmartDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAclService _aclService;
        private readonly CatalogSettings _catalogSettings;

        public RecentlyViewedProductsService(
            SmartDbContext db,
            IHttpContextAccessor httpContextAccessor,
            IAclService aclService,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _aclService = aclService;
            _catalogSettings = catalogSettings;
        }

        public virtual async Task<IList<Product>> GetRecentlyViewedProductsAsync(int number)
        {
            var productIds = GetRecentlyViewedProductsIds(number);

            if (!productIds.Any())
            {
                return new List<Product>();
            }

            var recentlyViewedProducts = await _db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ApplyStandardFilter()
                .ToListAsync();

            var authorizedProducts = await _aclService
                .SelectAuthorizedAsync(recentlyViewedProducts)
                .AsyncToList();

            return authorizedProducts.OrderBySequence(productIds).ToList();
        }

        public virtual void AddProductToRecentlyViewedList(int productId)
        {
            if (!_catalogSettings.RecentlyViewedProductsEnabled || _httpContextAccessor.HttpContext is null)
            {
                return;
            }

            var existingProductIds = GetRecentlyViewedProductsIds(int.MaxValue);
            var newProductIds = new List<int>(existingProductIds);

            newProductIds.Remove(productId);
            newProductIds.Insert(0, productId);

            var maxProducts = _catalogSettings.RecentlyViewedProductsNumber;
            if (maxProducts <= 0)
            {
                maxProducts = 8;
            }

            var cookies = _httpContextAccessor.HttpContext.Response.Cookies;
            var cookieName = CookieNames.RecentlyViewedProducts;

            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(10.0),
                HttpOnly = true,
                IsEssential = true
            };

            cookies.Delete(cookieName, options);

            cookies.Append(cookieName,
                string.Join(",", newProductIds.Take(maxProducts)),
                options);
        }

        protected virtual IEnumerable<int> GetRecentlyViewedProductsIds(int number)
        {
            var request = _httpContextAccessor?.HttpContext?.Request;

            if (request != null && request.Cookies.TryGetValue(CookieNames.RecentlyViewedProducts, out var values) && values.HasValue())
            {
                var ids = values.ToIntArray();

                if (!ids.Any())
                {
                    // Backward compatibility.
                    ids = values
                        .Split('&')
                        .Select(x => x.Split('=').Skip(1).FirstOrDefault().ToInt())
                        .Where(x => x != 0)
                        .ToArray();
                }

                return ids.Distinct().Take(number);
            }

            return Enumerable.Empty<int>();
        }
    }
}
