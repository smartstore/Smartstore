using Autofac;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Catalog.Products;
using Smartstore.Engine;
using System;
using System.Threading.Tasks;

namespace Smartstore
{
    public static class DeliveryTimeServiceExtensions
    {
        /// <summary>
        /// Calculates the delivery date.
        /// </summary>
        /// <param name="deliveryTime">Delivery time.</param>
        /// <returns>Calculated minimum and maximum date.</returns>
        public static (DateTime? minDate, DateTime? maxDate) GetDeliveryDate(this IDeliveryTimeService service, DeliveryTime deliveryTime)
        {
            var dateTimeHelper = EngineContext.Current.Scope.Resolve<IDateTimeHelper>();
            var currentDate = TimeZoneInfo.ConvertTime(DateTime.UtcNow, dateTimeHelper.DefaultStoreTimeZone);

            return service.GetDeliveryDate(deliveryTime, currentDate);
        }

        /// <summary>
        /// Gets the product delivery time according to stock
        /// </summary>
        public static Task<DeliveryTime> GetDeliveryTimeAsync(this IDeliveryTimeService service, Product product, bool displayAccordingToStock)
        {
            var deliveryTimeId = product.GetDeliveryTimeIdAccordingToStock(displayAccordingToStock);
            return service.GetDeliveryTimeAsync(deliveryTimeId, true);
        }

        public static int GetDeliveryTimeIdAccordingToStock(this Product product, bool displayAccordingToStock)
        {
            // TODO: (MH) (core) Use product extension when it's ready.
            return 0;
        }
    }
}
