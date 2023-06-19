#nullable enable

using Microsoft.AspNetCore.Http;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Net;

namespace Smartstore.Core.Catalog.Products
{
    public partial class RecentlyViewedProductsService : IRecentlyViewedProductsService
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CatalogSettings _catalogSettings;
        
        public RecentlyViewedProductsService(
            SmartDbContext db,
            IStoreContext storeContext,
            IWorkContext workContext,
            IHttpContextAccessor httpContextAccessor,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _storeContext = storeContext;
            _workContext = workContext;
            _httpContextAccessor = httpContextAccessor;
            _catalogSettings = catalogSettings;
        }

        public virtual async Task<IList<Product>> GetRecentlyViewedProductsAsync(
            int count,
            int[]? excludedProductIds = null,
            int? storeId = null)
        {
            storeId ??= _storeContext.CurrentStore.Id;

            var productIds = GetRecentlyViewedProductsIds(count * 2, excludedProductIds);
            if (!productIds.Any())
            {
                return new List<Product>();
            }

            var recentlyViewedProducts = await _db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ApplyStandardFilter()
                .ApplyStoreFilter(storeId.Value)
                .ApplyAclFilter(_workContext.CurrentCustomer)
                .SelectSummary()
                .OrderBy(x => x.Id)
                .Take(count)
                .ToListAsync();

            return recentlyViewedProducts.OrderBySequence(productIds).ToList();
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

            var maxProducts = GetRecentlyViewedProductsNumber();
            var cookies = _httpContextAccessor.HttpContext.Response.Cookies;

            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(10.0),
                HttpOnly = true,
                IsEssential = true
            };

            cookies.Delete(CookieNames.RecentlyViewedProducts, options);

            cookies.Append(CookieNames.RecentlyViewedProducts,
                string.Join(',', newProductIds.Take(maxProducts)),
                options);
        }

        protected virtual IEnumerable<int> GetRecentlyViewedProductsIds(int count, int[]? excludedProductIds = null)
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

                if (!excludedProductIds.IsNullOrEmpty())
                {
                    ids = ids.Where(x => !excludedProductIds!.Contains(x)).ToArray();
                }

                return ids.Distinct().Take(count);
            }

            return Enumerable.Empty<int>();
        }

        protected virtual int GetRecentlyViewedProductsNumber()
        {
            var maxProducts = _catalogSettings.RecentlyViewedProductsNumber;
            if (maxProducts <= 0)
            {
                maxProducts = 8;
            }

            // INFO: save one more product than needed, so that also on the product detail page
            // (where the current product is excluded) up to "RecentlyViewedProductsNumber" products are displayed.
            return (maxProducts + 1) * _storeContext.GetAllStores().Count;
        }
    }
}
