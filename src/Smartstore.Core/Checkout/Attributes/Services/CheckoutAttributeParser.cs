using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Attributes.Domain;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Attributes
{
    public partial class CheckoutAttributeParser : ICheckoutAttributeParser
    {
        private readonly SmartDbContext _db;

        public CheckoutAttributeParser(SmartDbContext db)
        {
            _db = db;
        }

        public Task<List<CheckoutAttribute>> ParseCheckoutAttributesAsync(CheckoutAttributeSelection selection)
        {
            var ids = selection.AttributesMap.Select(x => x.Key);
            return _db.CheckoutAttributes.GetManyAsync(ids);
        }

        public Task<List<CheckoutAttributeValue>> ParseCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection)
        {
            // TODO: (ms) (core) finish this
            return Task.FromResult(new List<CheckoutAttributeValue>());
        }
    }
}