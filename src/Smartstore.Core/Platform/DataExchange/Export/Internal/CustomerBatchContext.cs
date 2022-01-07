using Smartstore.Collections;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.DataExchange.Export.Internal
{
    internal class CustomerBatchContext
    {
        protected readonly List<int> _customerIds = new();

        protected readonly SmartDbContext _db;

        private LazyMultimap<GenericAttribute> _genericAttributes;

        public CustomerBatchContext(IEnumerable<Customer> customers, ICommonServices services)
        {
            Guard.NotNull(services, nameof(services));

            _db = services.DbContext;

            if (customers != null)
            {
                _customerIds.AddRange(customers.Select(x => x.Id));
            }
        }

        public IReadOnlyList<int> CustomerIds => _customerIds;

        public LazyMultimap<GenericAttribute> GenericAttributes
        {
            get => _genericAttributes ??=
                new LazyMultimap<GenericAttribute>(keys => LoadGenericAttributes(keys), _customerIds);
        }

        public virtual void Clear()
        {
            _genericAttributes?.Clear();
            _customerIds?.Clear();
        }

        #region Protected factories

        protected virtual async Task<Multimap<int, GenericAttribute>> LoadGenericAttributes(int[] customerIds)
        {
            var customerName = nameof(Customer);

            var genericAttributes = await _db.GenericAttributes
                .AsNoTracking()
                .Where(x => customerIds.Contains(x.EntityId) && x.KeyGroup == customerName)
                .ToListAsync();

            return genericAttributes.ToMultimap(x => x.EntityId, x => x);
        }

        #endregion
    }
}
