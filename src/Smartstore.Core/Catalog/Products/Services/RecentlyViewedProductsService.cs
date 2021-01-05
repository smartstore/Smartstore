using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Web;

namespace Smartstore.Core.Catalog.Products
{
    public partial class RecentlyViewedProductsService : IRecentlyViewedProductsService
    {
        private readonly SmartDbContext _db;
        private readonly IWebHelper _webHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CatalogSettings _catalogSettings;
        private readonly PrivacySettings _privacySettings;

        public RecentlyViewedProductsService(
            SmartDbContext db,
            IWebHelper webHelper,
            IHttpContextAccessor httpContextAccessor,
            CatalogSettings catalogSettings,
            PrivacySettings privacySettings)
        {
            _db = db;
            _webHelper = webHelper;
            _httpContextAccessor = httpContextAccessor;
            _catalogSettings = catalogSettings;
            _privacySettings = privacySettings;
        }

        public virtual async Task<IList<Product>> GetRecentlyViewedProductsAsync(int number)
        {
            var productIds = GetRecentlyViewedProductsIds(number);

            if (!productIds.Any())
            {
                return new List<Product>();
            }

            // TODO: (mg) (core) Apply IAclRestricted to recently viewed products query.
            var recentlyViewedProducts = await _db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ApplyStandardFilter()
                .ToListAsync();

            return recentlyViewedProducts;
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

            var skip = Math.Max(0, newProductIds.Count - maxProducts);
            var isSecured = _webHelper.IsCurrentConnectionSecured();
            var cookies = _httpContextAccessor.HttpContext.Response.Cookies;

            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(10.0),
                HttpOnly = true,
                Secure = isSecured,
                SameSite = isSecured ? (SameSiteMode)_privacySettings.SameSiteMode : SameSiteMode.Lax
            };

            cookies.Delete("SmartStore.RecentlyViewedProducts", options);

            cookies.Append("SmartStore.RecentlyViewedProducts", 
                string.Join(",", newProductIds.Skip(skip).Take(maxProducts)),
                options);
        }

        protected virtual IEnumerable<int> GetRecentlyViewedProductsIds(int number)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return Enumerable.Empty<int>();
            }
            
            // TODO: (core) Move all cookie names to a static util class.
            if (httpContext.Request.Cookies.TryGetValue("SmartStore.RecentlyViewedProducts", out var values) && values.HasValue())
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
