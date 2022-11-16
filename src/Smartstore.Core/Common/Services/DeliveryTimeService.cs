using System.Collections.Concurrent;
using System.Globalization;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Services
{
    [Important]
    public partial class DeliveryTimeService : AsyncDbSaveHook<DeliveryTime>, IDeliveryTimeService
    {
        private readonly static ConcurrentDictionary<string, string> _monthDayFormats = new(StringComparer.OrdinalIgnoreCase);

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

        #region Hook

        private string _hookErrorMessage;

        /// <summary>
        /// Sets all delivery times to <see cref="DeliveryTime.IsDefault"/> = false if the currently updated entity is the default delivery time.
        /// </summary>
        protected override async Task<HookResult> OnInsertingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetDefaultDeliveryTimes(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Sets all delivery times to <see cref="DeliveryTime.IsDefault"/> = false if the currently updated entity is the default delivery time.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetDefaultDeliveryTimes(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Prevents saving of delivery time if it's referenced in products or attribute combinations 
        /// and removes associations to deleted products and attribute combinations.
        /// </summary>
        protected override async Task<HookResult> OnDeletingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Remove associations to deleted products.
            var productsQuery = _db.Products
                .IgnoreQueryFilters()
                .Where(x => x.Deleted && x.DeliveryTimeId == entity.Id);

            var productsPager = new FastPager<Product>(productsQuery, 500);
            while ((await productsPager.ReadNextPageAsync<Product>(cancelToken)).Out(out var products))
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                if (products.Any())
                {
                    products.Each(x => x.DeliveryTimeId = null);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }

            // Remove associations to attribute combinations of deleted products.
            var attributeCombinationQuery =
                from ac in _db.ProductVariantAttributeCombinations
                join p in _db.Products.AsNoTracking().IgnoreQueryFilters() on ac.ProductId equals p.Id
                where p.Deleted && ac.DeliveryTimeId == entity.Id
                select ac;

            var attributeCombinationPager = new FastPager<ProductVariantAttributeCombination>(attributeCombinationQuery, 1000);
            while ((await attributeCombinationPager.ReadNextPageAsync<ProductVariantAttributeCombination>(cancelToken)).Out(out var attributeCombinations))
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                if (attributeCombinations.Any())
                {
                    attributeCombinations.Each(x => x.DeliveryTimeId = null);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }

            if (entity.IsDefault == true)
            {
                // Cannot delete the default delivery time.
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.DeliveryTimes.CannotDeleteDefaultDeliveryTime", entity.Name.NaIfEmpty());
            }
            else if (await _db.Products.AnyAsync(x => x.DeliveryTimeId == entity.Id || x.ProductVariantAttributeCombinations.Any(c => c.DeliveryTimeId == entity.Id), cancelToken))
            {
                // Cannot delete if there are associations to active products or attribute combinations.
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.DeliveryTimes.CannotDeleteAssignedProducts", entity.Name.NaIfEmpty());
            }

            return HookResult.Ok;
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
        }

        private async Task ResetDefaultDeliveryTimes(DeliveryTime deliveryTime, CancellationToken cancelToken)
        {
            Guard.NotNull(deliveryTime, nameof(deliveryTime));

            if (deliveryTime.IsDefault == true)
            {
                var deliveryTimes = await _db.DeliveryTimes
                    .Where(x => x.IsDefault == true && x.Id != deliveryTime.Id)
                    .ToListAsync(cancelToken);

                if (deliveryTimes.Any())
                {
                    deliveryTimes.Each(x => x.IsDefault = false);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }
        }

        #endregion

        public (DateTime? minDate, DateTime? maxDate) GetDeliveryDate(DeliveryTime deliveryTime)
        {
            var currentDate = TimeZoneInfo.ConvertTime(DateTime.UtcNow, _dateTimeHelper.DefaultStoreTimeZone);
            return GetDeliveryDate(deliveryTime, currentDate);
        }

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

        /// <see href="https://stackoverflow.com/questions/1044688/addbusinessdays-and-getbusinessdays"/>
        /// <seealso href="https://en.wikipedia.org/wiki/Workweek_and_weekend"/>
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
