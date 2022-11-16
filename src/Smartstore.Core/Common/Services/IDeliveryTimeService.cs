using System.Globalization;
using Smartstore.Core.Catalog;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Delivery time service interface
    /// </summary>
    public interface IDeliveryTimeService
    {
        /// <summary>
        /// Calculates the delivery date.
        /// </summary>
        /// <param name="deliveryTime">Delivery time.</param>
        /// <returns>Calculated minimum and maximum date.</returns>
        (DateTime? minDate, DateTime? maxDate) GetDeliveryDate(DeliveryTime deliveryTime);

        /// <summary>
        /// Calculates the delivery date.
        /// </summary>
        /// <param name="deliveryTime">Delivery time.</param>
        /// <param name="fromDate">The date from which the delivery date should be calculated.</param>
        /// <returns>Calculated minimum and maximum date.</returns>
        (DateTime? minDate, DateTime? maxDate) GetDeliveryDate(DeliveryTime deliveryTime, DateTime fromDate);

        /// <summary>
        /// Gets the formatted delivery date.
        /// </summary>
        /// <param name="deliveryTime">Delivery time.</param>
        /// <param name="fromDate">The date from which the delivery date should be calculated.
        /// <c>null</c> to use store's local time <see cref="DateTimeHelper.DefaultStoreTimeZone"/>.</param>
        /// <param name="culture">Culture to use for formatting. <c>null</c> to use UI culture of current thread.</param>
        /// <returns>Formatted delivery date.</returns>
        string GetFormattedDeliveryDate(DeliveryTime deliveryTime, DateTime? fromDate = null, CultureInfo culture = null);

        /// <summary>
        /// Gets a delivery time by id or returns default delivery time corresponding to <see cref="CatalogSettings.ShowDefaultDeliveryTime"/>
        /// </summary>
        /// <returns>DeliveryTime</returns>
        Task<DeliveryTime> GetDeliveryTimeAsync(int? deliveryTimeId, bool tracked = false);
    }
}