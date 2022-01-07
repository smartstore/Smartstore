using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Common.Services
{
    public partial class DateTimeHelper : IDateTimeHelper
    {
        private readonly SmartDbContext _db;
        private readonly ISettingService _settingService;
        private readonly IWorkContext _workContext;
        private readonly DateTimeSettings _dateTimeSettings;

        private TimeZoneInfo _cachedUserTimeZone;

        public DateTimeHelper(
            SmartDbContext db,
            ISettingService settingService,
            IWorkContext workContext,
            DateTimeSettings dateTimeSettings)
        {
            _db = db;
            _settingService = settingService;
            _workContext = workContext;
            _dateTimeSettings = dateTimeSettings;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual TimeZoneInfo FindTimeZoneById(string id)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual ReadOnlyCollection<TimeZoneInfo> GetSystemTimeZones()
        {
            return TimeZoneInfo.GetSystemTimeZones();
        }

        public virtual DateTime ConvertToUserTime(DateTime dt, DateTimeKind sourceDateTimeKind)
        {
            return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(dt, sourceDateTimeKind), CurrentTimeZone);
        }

        public virtual DateTime ConvertToUserTime(DateTime dt, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
        {
            return TimeZoneInfo.ConvertTime(dt, sourceTimeZone, destinationTimeZone);
        }

        public virtual DateTime ConvertToUtcTime(DateTime dt, TimeZoneInfo sourceTimeZone)
        {
            if (sourceTimeZone.IsInvalidTime(dt))
            {
                //could not convert
                return dt;
            }
            else
            {
                return TimeZoneInfo.ConvertTimeToUtc(dt, sourceTimeZone);
            }
        }

        public virtual TimeZoneInfo GetCustomerTimeZone(Customer customer)
        {
            if (_cachedUserTimeZone != null)
                return _cachedUserTimeZone;

            // registered user
            TimeZoneInfo timeZone = null;
            if (_dateTimeSettings.AllowCustomersToSetTimeZone)
            {
                string timeZoneId = string.Empty;
                if (customer != null)
                    timeZoneId = customer.TimeZoneId;

                try
                {
                    if (timeZoneId.HasValue())
                        timeZone = FindTimeZoneById(timeZoneId);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.ToString());
                }
            }

            // default timezone
            if (timeZone == null)
                timeZone = this.DefaultStoreTimeZone;

            _cachedUserTimeZone = timeZone;

            return timeZone;
        }

        public virtual TimeZoneInfo DefaultStoreTimeZone
        {
            get
            {
                TimeZoneInfo timeZoneInfo = null;
                try
                {
                    if (_dateTimeSettings.DefaultStoreTimeZoneId.HasValue())
                        timeZoneInfo = FindTimeZoneById(_dateTimeSettings.DefaultStoreTimeZoneId);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.ToString());
                }

                if (timeZoneInfo == null)
                    timeZoneInfo = TimeZoneInfo.Local;

                return timeZoneInfo;
            }
            set
            {
                string defaultTimeZoneId = string.Empty;
                if (value != null)
                {
                    defaultTimeZoneId = value.Id;
                }

                _dateTimeSettings.DefaultStoreTimeZoneId = defaultTimeZoneId;

                _settingService.ApplySettingAsync(_dateTimeSettings, x => x.DefaultStoreTimeZoneId).Await();
                _db.SaveChanges();

                _cachedUserTimeZone = null;
            }
        }

        public virtual TimeZoneInfo CurrentTimeZone
        {
            get => GetCustomerTimeZone(_workContext.CurrentCustomer);
            set
            {
                if (!_dateTimeSettings.AllowCustomersToSetTimeZone)
                    return;

                string timeZoneId = string.Empty;
                if (value != null)
                {
                    timeZoneId = value.Id;
                }

                _workContext.CurrentCustomer.TimeZoneId = timeZoneId;

                _db.SaveChanges();

                _cachedUserTimeZone = null;
            }
        }
    }
}