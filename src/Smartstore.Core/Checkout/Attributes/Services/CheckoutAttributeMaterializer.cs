using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Attributes
{
    public partial class CheckoutAttributeMaterializer : ICheckoutAttributeMaterializer
    {
        private readonly SmartDbContext _db;

        public CheckoutAttributeMaterializer(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<List<CheckoutAttribute>> MaterializeCheckoutAttributesAsync(CheckoutAttributeSelection selection)
        {
            Guard.NotNull(selection, nameof(selection));

            var ids = selection.AttributesMap.Select(x => x.Key).ToArray();

            if (!ids.Any())
            {
                return new List<CheckoutAttribute>();
            }

            return await _db.CheckoutAttributes
                .Include(x => x.CheckoutAttributeValues)
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
        }

        public async Task<List<CheckoutAttributeValue>> MaterializeCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection)
        {
            Guard.NotNull(selection, nameof(selection));

            var attributeIds = selection.AttributesMap.Select(x => x.Key).ToArray();
            if (!attributeIds.Any())
            {
                return new List<CheckoutAttributeValue>();
            }

            // AttributesMap can also contain numeric values of text fields that are not CheckoutAttributeValue IDs!
            var numericValues = selection.AttributesMap
                .SelectMany(x => x.Value)
                .Select(x => x.ToString())
                .Where(x => x.HasValue())
                .Select(x => x.ToInt())
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            if (!numericValues.Any())
            {
                return new List<CheckoutAttributeValue>();
            }

            var values = await _db.CheckoutAttributeValues
                .AsNoTracking()
                .Include(x => x.CheckoutAttribute)
                .Where(x => attributeIds.Contains(x.CheckoutAttributeId) && numericValues.Contains(x.Id))
                .ApplyListTypeFilter()
                .ToListAsync();

            return values;
        }

        public async Task<List<CheckoutAttribute>> GetCheckoutAttributesAsync(IEnumerable<OrganizedShoppingCartItem> cart, int storeId = 0)
        {
            Guard.NotNull(cart, nameof(cart));

            var checkoutAttributes = await _db.CheckoutAttributes
                .AsNoTracking()
                .ApplyStandardFilter(false, storeId)
                .ToListAsync();

            if (!cart.IncludesMatchingItems(x => x.IsShippingEnabled))
            {
                // Remove attributes which require shippable products.
                checkoutAttributes = checkoutAttributes
                    .Where(x => !x.ShippableProductRequired)
                    .ToList();
            }

            return checkoutAttributes;
        }
    }
}