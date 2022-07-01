using Smartstore.Core.Common;

namespace Smartstore
{
    public static class DeliveryTimeExtensions
    {
        /// <summary>
        /// Intended to be used for submission of products to third parties which require the delivery days as integer,
        /// e.g. price comparison portals or after-sales services.
        /// </summary>
        /// <returns>Days as integer or null if no days are set for the current delivery time.</returns>
        public static int? GetMinOrMaxOrAverageDays(this DeliveryTime deliveryTime)
        {
            Guard.NotNull(deliveryTime, nameof(deliveryTime));

            var minDays = deliveryTime.MinDays;
            var maxDays = deliveryTime.MaxDays;

            if (minDays > 0 && maxDays > 0)
            {
                return ((int)minDays + (int)maxDays) / 2;
            }
            else if (minDays > 0)
            {
                return (int)minDays;
            }
            else if (maxDays > 0)
            {
                return (int)maxDays;
            }

            return null;
        }
    }
}
