using System;
using System.Linq;
using Smartstore.Core.Web;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Payment
{
    // TODO: (mg) (core) Complete IPaymentMethod extensions.
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
            Guard.NotNull(paymentMethod, nameof(paymentMethod));
            Guard.NotNull(paymentSettings, nameof(paymentSettings));

            if (paymentSettings.ActivePaymentMethodSystemNames == null || !paymentMethod.Value.IsActive)
            {
                return false;
            }

            return paymentSettings.ActivePaymentMethodSystemNames.Contains(paymentMethod.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
        }

        public static RouteInfo GetConfigurationRoute(this IPaymentMethod method)
        {
            Guard.NotNull(method, nameof(method));

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

            //method.GetPaymentInfoRoute(out var action, out var controller, out var routeValues);
            //return action.HasValue() ? new RouteInfo(action, controller, routeValues) : null;

            return null;
        }
    }
}