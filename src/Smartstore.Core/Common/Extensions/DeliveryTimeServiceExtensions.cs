using Autofac;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Products;
using Smartstore.Engine;
using SmartStore.Services.Helpers;
using System;

namespace Smartstore
{
    public static class DeliveryTimeServiceExtensions
    {
        /// <summary>
        /// Calls corresponding service method <see cref="DeliveryTimeService.GetDeliveryDate(DeliveryTime, DateTime)"/>
        /// </summary>
        public static (DateTime? minDate, DateTime? maxDate) GetDeliveryDate(this IDeliveryTimeService service, DeliveryTime deliveryTime)
        {
            var dateTimeHelper = EngineContext.Current.Application.Services.Resolve<IDateTimeHelper>();
            var currentDate = TimeZoneInfo.ConvertTime(DateTime.UtcNow, dateTimeHelper.DefaultStoreTimeZone);

            return service.GetDeliveryDate(deliveryTime, currentDate);
        }

        /// <summary>
        /// Gets the product delivery time according to stock
        /// </summary>
        public static DeliveryTime GetDeliveryTime(this IDeliveryTimeService service, Product product, bool displayAccordingToStock)
        {
            var deliveryTimeId = product.GetDeliveryTimeIdAccordingToStock(displayAccordingToStock);
            var deliveryTime = service.GetDeliveryTimeAsync(deliveryTimeId, true).Await();

            return deliveryTime;
        }

        public static int GetDeliveryTimeIdAccordingToStock(this Product product, bool displayAccordingToStock)
        {
            // TODO: MH (core) Use product extension when it's ready.
            return 0;
        }
    }
}
