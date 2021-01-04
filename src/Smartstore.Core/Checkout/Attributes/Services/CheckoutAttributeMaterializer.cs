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

        public Task<List<CheckoutAttributeValue>> MaterializeCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection)
        {
            // TODO: (ms) (core) finish this
            return Task.FromResult(new List<CheckoutAttributeValue>());
        }
    }
}