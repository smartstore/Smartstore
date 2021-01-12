using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
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

        public Task<List<CheckoutAttribute>> MaterializeCheckoutAttributesAsync(CheckoutAttributeSelection selection)
        {
            var ids = selection.AttributesMap.Select(x => x.Key);
            return _db.CheckoutAttributes.GetManyAsync(ids);
        }

        public async Task<List<CheckoutAttributeValue>> MaterializeCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection)
        {
            var attributes = await MaterializeCheckoutAttributesAsync(selection);
            var valueIds = attributes.SelectMany(x => x.CheckoutAttributeValues.Select(y => y.Id));
            return await _db.CheckoutAttributeValues.GetManyAsync(valueIds);
        }
    }
}