using System.Diagnostics;
using Smartstore.Core.Checkout.Orders;
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
        public ShoppingCart(Customer customer, int storeId, IEnumerable<OrganizedShoppingCartItem> items)
        {
            Customer = Guard.NotNull(customer);
            Items = Guard.NotNull(items?.ToArray());
            StoreId = storeId;
        }

        public ShoppingCart(ShoppingCart other, IEnumerable<OrganizedShoppingCartItem> items = null)
        {
            Guard.NotNull(other);

            Customer = other.Customer;
            Items = items?.ToArray() ?? other.Items;
            StoreId = other.StoreId;
            CartType = other.CartType;
            Requirements = other.Requirements;
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

        /// <summary>
        /// Gets a value indicating whether the cart requires shipping.
        /// </summary>
        public bool IsShippingRequired
            => Requirements.HasFlag(CheckoutRequirements.Shipping) && Items.Any(x => x.Item.IsShippingEnabled);

        /// <summary>
        /// Checkout requirements.
        /// </summary>
        public CheckoutRequirements Requirements { get; init; } = CheckoutRequirements.All;

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
            var attributes = Customer.GenericAttributes;

            // INFO: the hash must be recreated for each call because modified customer attributes
            // like DiscountCouponCode can affect the same cart object.
            var combiner = HashCodeCombiner
                .Start()
                .Add(StoreId)
                .Add(CartType)
                .Add(Customer.Id)
                .Add(attributes?.DiscountCouponCode)
                .Add(attributes?.RawGiftCardCouponCodes)
                .Add(attributes?.RawCheckoutAttributes)
                .Add(attributes?.VatNumber)
                .Add(attributes?.UseRewardPointsDuringCheckout)
                .Add(attributes?.UseCreditBalanceDuringCheckout)
                .AddSequence(Items.Select(x => x.Item));

            return combiner.CombinedHash;
        }

        #endregion
    }
}
