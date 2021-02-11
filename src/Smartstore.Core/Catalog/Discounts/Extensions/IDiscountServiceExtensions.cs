using System.Threading.Tasks;
using Smartstore.Core.Identity;

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
        /// <returns><c>true</c>discount requirements are met, otherwise <c>false</c>.</returns>
        public static async Task<bool> IsDiscountValidAsync(this IDiscountService discountService, Discount discount, Customer customer)
        {
            Guard.NotNull(discountService, nameof(discountService));

            var couponCodeToValidate = customer?.GenericAttributes?.DiscountCouponCode ?? string.Empty;

            return await discountService.IsDiscountValidAsync(discount, customer, couponCodeToValidate);
        }
    }
}
