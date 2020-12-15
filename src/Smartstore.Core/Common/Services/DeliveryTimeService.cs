using System;
using Smartstore.Core.Data;
using Smartstore.Core.Common.Settings;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading;
using SmartStore.Core.Shipping.Settings;
using System.Collections.Concurrent;
using Smartstore.Core.Localization;
using SmartStore.Core.Domain.Catalog;

namespace Smartstore.Core.Common.Services
{
    public partial class DeliveryTimeService : IDeliveryTimeService
    {
        private readonly static ConcurrentDictionary<string, string> _monthDayFormats = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly SmartDbContext _db;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ShippingSettings _shippingSettings;
        private readonly CatalogSettings _catalogSettings;
        
        public DeliveryTimeService(
            SmartDbContext db, 
            IDateTimeHelper dateTimeHelper,
            ShippingSettings shippingSettings,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _dateTimeHelper = dateTimeHelper;
            _shippingSettings = shippingSettings;
            _catalogSettings = catalogSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual (DateTime? minDate, DateTime? maxDate) GetDeliveryDate(DeliveryTime deliveryTime, DateTime fromDate)
        {
            var minDate = deliveryTime?.MinDays != null
                ? AddDays(fromDate, deliveryTime.MinDays.Value)
                : (DateTime?)null;

            var maxDate = deliveryTime?.MaxDays != null
                ? AddDays(fromDate, deliveryTime.MaxDays.Value)
                : (DateTime?)null;

            return (minDate, maxDate);
        }

        public virtual string GetFormattedDeliveryDate(DeliveryTime deliveryTime, DateTime? fromDate = null, CultureInfo culture = null)
        {
            if (deliveryTime == null || (!deliveryTime.MinDays.HasValue && !deliveryTime.MaxDays.HasValue))
            {
                return null;
            }

            if (culture == null)
            {
                culture = Thread.CurrentThread.CurrentUICulture;
            }

            var currentDate = fromDate ?? TimeZoneInfo.ConvertTime(DateTime.UtcNow, _dateTimeHelper.DefaultStoreTimeZone);
            var (min, max) = GetDeliveryDate(deliveryTime, currentDate);

            if (min.HasValue)
            {
                min = _dateTimeHelper.ConvertToUserTime(min.Value);
            }
            if (max.HasValue)
            {
                max = _dateTimeHelper.ConvertToUserTime(max.Value);
            }

            // Convention: always separate weekday with comma and format month in shortest form.
            if (min.HasValue && max.HasValue)
            {
                if (min == max)
                {
                    return T("DeliveryTimes.Dates.DeliveryOn", Format(min.Value, "dddd, ", false, "DeliveryTimes.Dates.OnTomorrow"));
                }
                else if (min < max)
                {
                    return T("DeliveryTimes.Dates.Between",
                        Format(min.Value, "ddd, ", min.Value.Month == max.Value.Month && min.Value.Year == max.Value.Year, null),
                        Format(max.Value, "ddd, ", false, null));
                }
            }
            else if (min.HasValue)
            {
                return T("DeliveryTimes.Dates.NotBefore", Format(min.Value, "dddd, "));
            }
            else if (max.HasValue)
            {
                return T("DeliveryTimes.Dates.Until", Format(max.Value, "dddd, "));
            }

            return null;

            string Format(DateTime date, string patternPrefix, bool noMonth = false, string tomorrowKey = "DeliveryTimes.Dates.Tomorrow")
            {
                // Offer some way to skip our formatting and to force a custom formatting.
                if (_shippingSettings.DeliveryTimesDateFormat.HasValue())
                {
                    return date.ToString(_shippingSettings.DeliveryTimesDateFormat, culture) ?? "-";
                }

                if (tomorrowKey != null && (date - currentDate).TotalDays == 1)
                {
                    return T(tomorrowKey);
                }

                string patternSuffix = null;

                if (noMonth)
                {
                    // MonthDayPattern can contain non-interpreted text like "de", "mh" or even "'d'" (e.g. 21 de septiembre).
                    patternSuffix = _monthDayFormats.GetOrAdd(culture.TwoLetterISOLanguageName + "-nomonth", _ =>
                    {
                        var mdp = culture.DateTimeFormat.MonthDayPattern;
                        return mdp.Contains("d. ") || mdp.Contains("dd. ") ? "d." : "d";
                    });
                }
                else
                {
                    patternSuffix = _monthDayFormats.GetOrAdd(culture.TwoLetterISOLanguageName, _ =>
                    {
                        return culture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM").TrimSafe();
                    });
                }

                return date.ToString(patternPrefix + patternSuffix, culture) ?? "-";
            }
        }

        public virtual async Task<DeliveryTime> GetDeliveryTimeAsync(int? deliveryTimeId, bool tracked = false)
        {
            if (deliveryTimeId == 0 || deliveryTimeId == null)
            {
                if (_catalogSettings.ShowDefaultDeliveryTime)
                {
                    return await _db.DeliveryTimes
                        .ApplyTracking(tracked)
                        .Where(x => x.IsDefault == true)
                        .FirstOrDefaultAsync();
                }

                return null;
            }

            return await _db.DeliveryTimes
                .ApplyTracking(tracked)
                .Where(x => x.Id == deliveryTimeId)
                .FirstOrDefaultAsync();
        }

        #region Utilities

        /// <see cref="https://stackoverflow.com/questions/1044688/addbusinessdays-and-getbusinessdays"/>
        /// <seealso cref="https://en.wikipedia.org/wiki/Workweek_and_weekend"/>
        protected virtual DateTime AddDays(DateTime date, int days)
        {
            Guard.NotNegative(days, nameof(days));

            // now.Hour: 0-23. TodayDeliveryHour: 1-24.
            if (_shippingSettings.TodayShipmentHour.HasValue && date.Hour < _shippingSettings.TodayShipmentHour)
            {
                if ((date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday) || !_shippingSettings.DeliveryOnWorkweekDaysOnly)
                {
                    days -= 1;
                }
            }

            // Normalization. Do not support today delivery.
            if (days < 1)
            {
                days = 1;
            }

            if (!_shippingSettings.DeliveryOnWorkweekDaysOnly)
            {
                return date.AddDays(days);
            }

            // Add days for non workweek days.
            if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                date = date.AddDays(2);
                days -= 1;
            }
            else if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
                days -= 1;
            }

            date = date.AddDays(days / 5 * 7);
            int extraDays = days % 5;

            if ((int)date.DayOfWeek + extraDays > 5)
            {
                extraDays += 2;
            }

            return date.AddDays(extraDays);
        }

        #endregion
    }
}
