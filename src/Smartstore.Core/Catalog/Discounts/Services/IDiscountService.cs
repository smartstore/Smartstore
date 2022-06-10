using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Discounts
{
    /// <summary>
    /// Discount service interface.
    /// </summary>
    public partial interface IDiscountService
    {
        /// <summary>
        /// Gets all discounts as untracked entities.
        /// </summary>
        /// <param name="discountType">Discount type. <c>null</c> to load all discounts.</param>
        /// <param name="couponCode">Discount coupon code. <c>null</c> to load all discounts.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden discounts.</param>
        /// <returns>Discounts.</returns>
        Task<IEnumerable<Discount>> GetAllDiscountsAsync(DiscountType? discountType, string couponCode = null, bool includeHidden = false);

        /// <summary>
        /// Checks whether the discount requirements are met.
        /// </summary>
        /// <param name="discount">Discount.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="couponCodeToValidate">Coupon code to validate.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns><c>true</c>discount requirements are met, otherwise <c>false</c>.</returns>
        Task<bool> IsDiscountValidAsync(Discount discount, Customer customer, string couponCodeToValidate, Store store = null);

        /// <summary>
        /// Applies given <paramref name="selectedDiscountIds"/> to <paramref name="entity"/>.
        /// The caller is responsible for database commit.
        /// </summary>
        /// <param name="entity">The entity to apply discounts to.</param>
        /// <param name="selectedDiscountIds">Identifiers of discounts to apply.</param>
        /// <param name="type">The discount type.</param>
        /// <returns><c>true</c> if a database commit is required. <c>false</c> if nothing changed.</returns>
        Task<bool> ApplyDiscountsAsync<T>(T entity, int[] selectedDiscountIds, DiscountType type) where T : BaseEntity, IDiscountable;
    }
}
