using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Discounts
{
    public static partial class IDiscountServiceExtensions
    {
        /// <summary>
        /// Checks whether the discount requirements are met.
        /// </summary>
        /// <param name="discountService">Discount service.</param>
        /// <param name="discount">Discount.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <param name="flags">
        /// Specifies which discount requirements to be validated.
        /// Requirements for which no further data needs to be loaded are always validated (e.g. coupon codes and date ranges).
        /// </param>
        /// <returns><c>true</c> discount requirements are met, otherwise <c>false</c>.</returns>
        public static Task<bool> IsDiscountValidAsync(this IDiscountService discountService,
            Discount discount,
            Customer customer,
            Store store = null,
            DiscountValidationFlags flags = DiscountValidationFlags.All)
        {
            Guard.NotNull(discountService);

            var couponCodeToValidate = customer?.GenericAttributes?.DiscountCouponCode ?? string.Empty;

            return discountService.IsDiscountValidAsync(discount, customer, couponCodeToValidate, store, flags);
        }
    }
}
