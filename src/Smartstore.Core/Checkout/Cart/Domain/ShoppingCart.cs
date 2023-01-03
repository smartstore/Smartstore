using System.Diagnostics;
using Smartstore.Core.Identity;
using Smartstore.Utilities;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a shopping cart.
    /// </summary>
    [DebuggerDisplay("{CartType} for {Customer.Email} contains {Items.Length} items.")]
    public partial class ShoppingCart : IEquatable<ShoppingCart>
    {
        private int? _hashCode;

        public ShoppingCart(Customer customer, int storeId, IEnumerable<OrganizedShoppingCartItem> items)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(items, nameof(items));

            Customer = customer;
            StoreId = storeId;
            Items = items.ToArray();
        }

        /// <summary>
        /// Array of cart items.
        /// </summary>
        public OrganizedShoppingCartItem[] Items { get; }

        /// <summary>
        /// A value indicating whether the cart contains cart items.
        /// </summary>
        public bool HasItems
            => Items.Length > 0;

        /// <summary>
        /// Shopping cart type.
        /// </summary>
        public ShoppingCartType CartType { get; init; } = ShoppingCartType.ShoppingCart;

        /// <summary>
        /// Customer of the cart.
        /// </summary>
        public Customer Customer { get; }

        /// <summary>
        /// Store identifier.
        /// </summary>
        public int StoreId { get; }

        #region Compare

        public override bool Equals(object obj)
            => Equals(obj as ShoppingCart);

        bool IEquatable<ShoppingCart>.Equals(ShoppingCart other)
            => Equals(other);

        protected virtual bool Equals(ShoppingCart other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (CartType != other.CartType 
                || Customer.Id != other.Customer.Id 
                || StoreId != other.StoreId
                || Items.Length != other.Items.Length)
            {
                return false;
            }

            return GetHashCode() == other.GetHashCode();
        }

        public override int GetHashCode()
        {
            if (_hashCode == null)
            {
                var combiner = HashCodeCombiner
                    .Start()
                    .Add((int)CartType)
                    .Add(Customer.Id)
                    .Add(StoreId)
                    .Add(Items.Select(x => x.Item.GetHashCode()));

                _hashCode = combiner.CombinedHash;
            }

            return _hashCode.Value;
        }

        #endregion
    }
}
