using System.Runtime.CompilerServices;
using Smartstore.Core.Common.Services;

namespace Smartstore
{
    public static class DateTimeHelperExtensions
    {
        /// <summary>
        /// Converts the date and time zone info to current user date and time
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ConvertToUserTime(this IDateTimeHelper helper, DateTime dt, TimeZoneInfo sourceTimeZone = null)
        {
            sourceTimeZone ??= helper.CurrentTimeZone;

            return helper.ConvertToUserTime(dt, sourceTimeZone, helper.CurrentTimeZone);
        }

        /// <summary>
        /// Converts the date to current user date and time
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ConvertToUserTime(this IDateTimeHelper helper, DateTime dt)
        {
            return helper.ConvertToUserTime(dt, dt.Kind);
        }

        /// <summary>
        /// Converts the date and time to Coordinated Universal Time (UTC)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ConvertToUtcTime(this IDateTimeHelper helper, DateTime dt, DateTimeKind sourceDateTimeKind, TimeZoneInfo sourceTimeZone = null)
        {
            sourceTimeZone ??= helper.CurrentTimeZone;
            dt = DateTime.SpecifyKind(dt, sourceDateTimeKind);
            return helper.ConvertToUtcTime(dt, sourceTimeZone);
        }

        /// <summary>
        /// Converts the date and time to Coordinated Universal Time (UTC)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ConvertToUtcTime(this IDateTimeHelper helper, DateTime dt)
        {
            return helper.ConvertToUtcTime(dt, dt.Kind);
        }
    }
}
