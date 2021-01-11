using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Web;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductCompareService : IProductCompareService
    {
        private readonly SmartDbContext _db;
        private readonly IWebHelper _webHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PrivacySettings _privacySettings;

        public ProductCompareService(
            SmartDbContext db,
            IWebHelper webHelper,
            IHttpContextAccessor httpContextAccessor,
            PrivacySettings privacySettings)
        {
            _db = db;
            _webHelper = webHelper;
            _httpContextAccessor = httpContextAccessor;
            _privacySettings = privacySettings;
        }

        public virtual int CountComparedProducts()
        {
            var productIds = GetComparedProductsIds();
            if (productIds.Any())
            {
                // TODO: (mg) (core) Complete GetComparedProductsCount (ICatalogSearchService required).
                //var searchQuery = new CatalogSearchQuery()
                //    .VisibleOnly()
                //    .WithProductIds(productIds.ToArray())
                //    .BuildHits(false);

                //var result = _catalogSearchService.Search(searchQuery);
                //return result.TotalHitsCount;
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

            return comparedProducts;
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
                var newProductIds = new List<int>(productId);

                newProductIds.AddRange(productIds
                    .Where(x => x != productId)
                    .Take(maxProducts - 1));

                SetComparedProductsIds(newProductIds);
            }
        }

        protected virtual IEnumerable<int> GetComparedProductsIds()
        {
            var request = _httpContextAccessor?.HttpContext?.Request;

            // TODO: (core) Move all cookie names to a static util class.
            if (request != null && request.Cookies.TryGetValue("Smartstore.CompareProducts", out var values) && values.HasValue())
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

                return ids.Distinct();
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

            var isSecured = _webHelper.IsCurrentConnectionSecured();
            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(10.0),
                HttpOnly = true,
                // TODO: (core) Check whether CookieOptions.Secure and .SameSite can be set via global policy.
                Secure = isSecured,
                SameSite = isSecured ? (SameSiteMode)_privacySettings.SameSiteMode : SameSiteMode.Lax
            };

            cookies.Delete("Smartstore.CompareProducts", options);

            if (productIds?.Any() ?? false)
            {
                cookies.Append("Smartstore.CompareProducts", string.Join(",", productIds), options);
            }
        }
    }
}
