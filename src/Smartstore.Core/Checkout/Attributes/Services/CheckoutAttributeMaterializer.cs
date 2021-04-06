using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Attributes
{
    public partial class CheckoutAttributeMaterializer : ICheckoutAttributeMaterializer
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;

        public CheckoutAttributeMaterializer(SmartDbContext db, IStoreContext storeContext)
        {
            _db = db;
            _storeContext = storeContext;
        }

        public Task<List<CheckoutAttribute>> MaterializeCheckoutAttributesAsync(CheckoutAttributeSelection selection)
        {
            var ids = selection.AttributesMap.Select(x => x.Key);
            return _db.CheckoutAttributes
                .Include(x => x.CheckoutAttributeValues)
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
        }

        public async Task<List<CheckoutAttributeValue>> MaterializeCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection)
        {
            var valueIds = selection.GetAttributeValueIds();

            var values = await _db.CheckoutAttributeValues
                .AsNoTracking()
                .Include(x => x.CheckoutAttribute)
                .Where(x => valueIds.Contains(x.Id))
                .ToListAsync();

            return values;
        }

        // TODO: (ms) (core) Move this method to a more appropriate space
        public async Task<List<CheckoutAttribute>> GetValidCheckoutAttributesAsync(IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            var checkoutAttributes = await _db.CheckoutAttributes
                .AsNoTracking()
                .ApplyStandardFilter(false, _storeContext.CurrentStore.Id)
                .ToListAsync();

            if (!cart.IncludesMatchingItems(x => x.IsShippingEnabled))
            {
                // Remove attributes which require shippable products.
                checkoutAttributes = checkoutAttributes.RemoveShippableAttributes().ToList();
            }

            return checkoutAttributes;
        }
    }
}