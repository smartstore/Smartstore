using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Identity;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Security;
using Smartstore.Core.Web;
using Smartstore.Net;

namespace Smartstore.Core.Catalog.Products
{
    public partial class RecentlyViewedProductsService : IRecentlyViewedProductsService
    {
        private readonly SmartDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAclService _aclService;
        private readonly CatalogSettings _catalogSettings;
        private readonly PrivacySettings _privacySettings;

        public RecentlyViewedProductsService(
            SmartDbContext db,
            IHttpContextAccessor httpContextAccessor,
            IAclService aclService,
            CatalogSettings catalogSettings,
            PrivacySettings privacySettings)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _aclService = aclService;
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

            var recentlyViewedProducts = await _db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ApplyStandardFilter()
                .ToListAsync();

            var authorizedProducts = await _aclService
                .SelectAuthorizedAsync(recentlyViewedProducts)
                .ToListAsync();

            return authorizedProducts;
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
                string.Join(",", newProductIds.Skip(skip).Take(maxProducts)),
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
