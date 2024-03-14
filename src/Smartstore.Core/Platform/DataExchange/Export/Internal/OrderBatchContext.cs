using Smartstore.Collections;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.DataExchange.Export.Internal
{
    internal class OrderBatchContext
    {
        protected readonly List<int> _orderIds = [];
        protected readonly List<int> _customerIds = [];
        protected readonly List<int> _addressIds = [];

        protected readonly SmartDbContext _db;

        private LazyMultimap<Customer> _customers;
        private LazyMultimap<GenericAttribute> _customerGenericAttributes;
        private LazyMultimap<RewardPointsHistory> _rewardPointsHistories;
        private LazyMultimap<Address> _addresses;
        private LazyMultimap<OrderItem> _orderItems;
        private LazyMultimap<Shipment> _shipments;

        public OrderBatchContext(IEnumerable<Order> orders, ICommonServices services)
        {
            Guard.NotNull(services);

            _db = services.DbContext;

            if (orders != null)
            {
                _orderIds.AddRange(orders.Select(x => x.Id));
                _customerIds.AddRange(orders.Select(x => x.CustomerId));

                _addressIds = orders
                    .Select(x => x.BillingAddressId ?? 0)
                    .Union(orders.Select(x => x.ShippingAddressId ?? 0))
                    .Where(x => x != 0)
                    .Distinct()
                    .ToList();
            }
        }

        public IReadOnlyList<int> OrderIds => _orderIds;

        public LazyMultimap<Customer> Customers
        {
            get => _customers ??=
                new LazyMultimap<Customer>(LoadCustomers, _customerIds);
        }

        public LazyMultimap<GenericAttribute> CustomerGenericAttributes
        {
            get => _customerGenericAttributes ??=
                new LazyMultimap<GenericAttribute>(LoadCustomerGenericAttributes, _customerIds);
        }

        public LazyMultimap<RewardPointsHistory> RewardPointsHistories
        {
            get => _rewardPointsHistories ??=
                new LazyMultimap<RewardPointsHistory>(LoadRewardPointsHistories, _customerIds);
        }

        public LazyMultimap<Address> Addresses
        {
            get => _addresses ??=
                new LazyMultimap<Address>(LoadAddresses, _addressIds);
        }

        public LazyMultimap<OrderItem> OrderItems
        {
            get => _orderItems ??=
                new LazyMultimap<OrderItem>(LoadOrderItems, _orderIds);
        }

        public LazyMultimap<Shipment> Shipments
        {
            get => _shipments ??=
                new LazyMultimap<Shipment>(LoadShipments, _orderIds);
        }

        public virtual void Clear()
        {
            _customers?.Clear();
            _customerGenericAttributes?.Clear();
            _rewardPointsHistories?.Clear();
            _addresses?.Clear();
            _orderItems?.Clear();
            _shipments?.Clear();

            _orderIds?.Clear();
            _customerIds?.Clear();
            _addressIds?.Clear();
        }

        #region Protected factories

        protected virtual async Task<Multimap<int, Customer>> LoadCustomers(int[] customerIds)
        {
            var customers = await _db.Customers
                .AsNoTrackingWithIdentityResolution()
                .IncludeCustomerRoles()
                .Where(x => customerIds.Contains(x.Id))
                .ToListAsync();

            return customers.ToMultimap(x => x.Id, x => x);
        }

        protected virtual async Task<Multimap<int, GenericAttribute>> LoadCustomerGenericAttributes(int[] customerIds)
        {
            var customerName = nameof(Customer);

            var genericAttributes = await _db.GenericAttributes
                .AsNoTracking()
                .Where(x => customerIds.Contains(x.EntityId) && x.KeyGroup == customerName)
                .ToListAsync();

            return genericAttributes.ToMultimap(x => x.EntityId, x => x);
        }

        protected virtual async Task<Multimap<int, RewardPointsHistory>> LoadRewardPointsHistories(int[] customerIds)
        {
            var rewardPointHistories = await _db.RewardPointsHistory
                .AsNoTracking()
                .ApplyCustomerFilter(customerIds)
                .ToListAsync();

            return rewardPointHistories.ToMultimap(x => x.CustomerId, x => x);
        }

        protected virtual async Task<Multimap<int, Address>> LoadAddresses(int[] addressIds)
        {
            var addresses = await _db.Addresses
                .AsNoTracking()
                .Where(x => addressIds.Contains(x.Id))
                .ToListAsync();

            return addresses.ToMultimap(x => x.Id, x => x);
        }

        protected virtual async Task<Multimap<int, OrderItem>> LoadOrderItems(int[] orderIds)
        {
            var orderItems = await _db.OrderItems
                .AsNoTrackingWithIdentityResolution()
                .Include(x => x.Product)
                .Where(x => orderIds.Contains(x.OrderId))
                .OrderBy(x => x.OrderId)
                .ToListAsync();

            return orderItems.ToMultimap(x => x.OrderId, x => x);
        }

        protected virtual async Task<Multimap<int, Shipment>> LoadShipments(int[] orderIds)
        {
            var shipments = await _db.Shipments
                .AsNoTracking()
                .Include(x => x.ShipmentItems)
                .ApplyOrderFilter(orderIds)
                .ToListAsync();

            return shipments.ToMultimap(x => x.OrderId, x => x);
        }

        #endregion
    }
}
