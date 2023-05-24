using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Catalog.Discounts
{
    /// <summary>
    /// Represents a usage history item for discounts.
    /// </summary>
    public partial class DiscountUsageHistory : BaseEntity
    {
        /// <summary>
        /// Gets or sets the discount identifier.
        /// </summary>
        public int DiscountId { get; set; }

        private Discount _discount;
        /// <summary>
        /// Gets or sets the discount.
        /// </summary>
        public Discount Discount
        {
            get => _discount ?? LazyLoader.Load(this, ref _discount);
            set => _discount = value;
        }

        /// <summary>
        /// Gets or sets the order identifier.
        /// </summary>
        public int OrderId { get; set; }

        private Order _order;
        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        public Order Order
        {
            get => _order ?? LazyLoader.Load(this, ref _order);
            set => _order = value;
        }

        /// <summary>
        /// Gets or sets the date of instance creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
    }
}
