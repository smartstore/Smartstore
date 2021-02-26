using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Web;
using Smartstore.Engine.Modularity;

namespace Smartstore
{
    public static class PaymentExtentions
    {
        /// <summary>
        /// Checks whether payment method is active.
        /// </summary>
        /// <param name="paymentMethod">Payment method.</param>
        /// <param name="paymentSettings">Payment settings.</param>
        /// <returns><c>True</c> if payment method is active, otherwise <c>false</c>.</returns>
        public static bool IsPaymentMethodActive(this Provider<IPaymentMethod> paymentMethod, PaymentSettings paymentSettings)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            if (paymentSettings == null)
                throw new ArgumentNullException(nameof(paymentSettings));

            if (paymentSettings.ActivePaymentMethodSystemNames == null
                || !paymentMethod.Value.IsActive)
                return false;

            return paymentSettings.ActivePaymentMethodSystemNames.Contains(paymentMethod.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Calculate payment method fee.
        /// </summary>
        /// <param name="paymentMethod">Payment method.</param>
        /// <param name="orderCalculationService">Order calculation service.</param>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="fee">Fee value.</param>
        /// <param name="usePercentage">Is fee amount specified as percentage or fixed value?</param>
        /// <returns>Result</returns>
        public static async Task<decimal> CalculateAdditionalFee(
            this IPaymentMethod paymentMethod,
            IOrderCalculationService orderCalculationService,
            IList<OrganizedShoppingCartItem> cart,
            decimal fee,
            bool usePercentage)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            if (fee == decimal.Zero)
                return fee;

            decimal result;
            if (usePercentage)
            {
                // Percentage
                var orderTotalWithoutPaymentFee = await orderCalculationService.GetShoppingCartTotalAsync(cart, includePaymentAdditionalFee: false);
                result = (decimal)orderTotalWithoutPaymentFee.TotalAmount * fee / 100m;
            }
            else
            {
                // Fixed value
                result = fee;
            }
            return result;
        }

        public static RouteInfo GetConfigurationRoute(this IPaymentMethod method)
        {
            Guard.NotNull(method, nameof(method));

            // TODO: (core) (ms) GetConfigurationRoute is missing

            //if (method is IConfigurable configurable)
            //{
            //    configurable.GetConfigurationRoute(out var action, out var controller, out var routeValues);
            //    if (action.HasValue())
            //    {
            //        return new RouteInfo(action, controller, routeValues);
            //    }
            //}

            return null;
        }

        public static RouteInfo GetPaymentInfoRoute(this IPaymentMethod method)
        {
            Guard.NotNull(method, nameof(method));

            // TODO: (core) (ms) GetPaymentInfoRoute is missing

            //method.GetPaymentInfoRoute(out var action, out var controller, out var routeValues);
            //return action.HasValue() ? new RouteInfo(action, controller, routeValues) : null;

            return null;
        }
    }
}