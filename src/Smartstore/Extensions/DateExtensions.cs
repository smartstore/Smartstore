#nullable enable

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using Humanizer;

namespace Smartstore
{
    public static class DateExtensions
    {
        public static readonly DateTime BeginOfEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        extension(DateTime? value)
        {
            /// <summary>
            /// Converts a nullable date/time value to UTC.
            /// </summary>
            /// <param name="value">The nullable date/time</param>
            /// <returns>The nullable date/time in UTC</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DateTime? ToUniversalTime()
            {
                return value.HasValue ? value.Value.ToUniversalTime() : null;
            }

            /// <summary>
            /// Converts a nullable UTC date/time value to local time.
            /// </summary>
            /// <param name="value">The nullable UTC date/time</param>
            /// <returns>The nullable UTC date/time as local time</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DateTime? ToLocalTime()
            {
                return value.HasValue ? value.Value.ToLocalTime() : null;
            }

            /// <summary>
            /// Returns a date that is rounded to the next even hour above the given date.
            /// </summary>
            /// <param name="value">the Date to round, if <see langword="null" /> the current time will be used</param>
            /// <returns>the new rounded date</returns>
            public DateTime GetEvenHourDate()
            {
                if (!value.HasValue)
                {
                    value = DateTime.UtcNow;
                }

                DateTime d = value.Value.AddHours(1);
                return new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0);
            }

            /// <summary>
            /// Returns a date that is rounded to the next even minute above the given date.
            /// </summary>
            /// <param name="value">The Date to round, if <see langword="null" /> the current time will  be used</param>
            /// <returns>The new rounded date</returns>
            public DateTime GetEvenMinuteDate()
            {
                if (!value.HasValue)
                {
                    value = DateTime.UtcNow;
                }

                DateTime d = value.Value;
                d = d.AddMinutes(1);
                return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
            }

            /// <summary>
            /// Returns a date that is rounded to the previous even minute below the given date.
            /// </summary>
            /// <param name="value">the Date to round, if <see langword="null" /> the current time will be used</param>
            /// <returns>the new rounded date</returns>
            public DateTime GetEvenMinuteDateBefore()
            {
                if (!value.HasValue)
                {
                    value = DateTime.UtcNow;
                }

                DateTime d = value.Value;
                return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
            }

            public DateTime? ToEndOfTheDay()
            {
                return value.HasValue ? value.Value.ToEndOfTheDay() : value;
            }

            /// <inheritdoc cref="DateHumanizeExtensions.Humanize(DateTime?, bool?, DateTime?, CultureInfo)"/>
            public string ToHumanizedString(bool? utcDate = null, DateTime? dateToCompareAgainst = null)
            {
                try
                {
                    return value.Humanize(utcDate, dateToCompareAgainst);
                }
                catch (ArgumentException)
                {
                    return value.Humanize(utcDate, dateToCompareAgainst, CultureInfo.InvariantCulture);
                }
            }
        }

        extension(DateTime value)
        {
            public long ToJavaScriptTicks()
            {
                DateTimeOffset utcDateTime = value.ToUniversalTime();
                long javaScriptTicks = (utcDateTime.Ticks - BeginOfEpoch.Ticks) / (long)10000;
                return javaScriptTicks;
            }

            /// <summary>
            /// Get the first day of the month for any full date submitted.
            /// </summary>
            public DateTime GetFirstDayOfMonth()
            {
                DateTime dtFrom = value;
                dtFrom = dtFrom.AddDays(-(dtFrom.Day - 1));
                return dtFrom;
            }

            /// <summary>
            /// Get the last day of the month for any full date.
            /// </summary>
            public DateTime GetLastDayOfMonth()
            {
                DateTime dtTo = value;
                dtTo = dtTo.AddMonths(1);
                dtTo = dtTo.AddDays(-(dtTo.Day));
                return dtTo;
            }

            public DateTime ToEndOfTheDay()
            {
                return new DateTime(value.Year, value.Month, value.Day, 23, 59, 59);
            }

            /// <summary>
            /// Epoch time. Number of seconds since midnight (UTC) on 1st January 1970.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public long ToUnixTime()
            {
                return Convert.ToInt64((value.ToUniversalTime() - BeginOfEpoch).TotalSeconds);
            }

            /// <summary>
            /// Converts a DateTime to a string with native digits
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string ToNativeString(IFormatProvider? provider = null)
                => FormatWithNativeDigits(value, null, provider);

            /// <summary>
            /// Converts a DateTime to a string with native digits
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string ToNativeString(string? format, IFormatProvider? provider = null)
                => FormatWithNativeDigits(value, format, provider);

            /// <summary>
            /// Converts a DateTime to ISO 8601 string
            /// </summary>
            public string ToIso8601String()
                => value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            /// <inheritdoc cref="DateHumanizeExtensions.Humanize(DateTime, bool?, DateTime?, CultureInfo)"/>
            public string ToHumanizedString(bool? utcDate = null, DateTime? dateToCompareAgainst = null)
            {
                try
                {
                    return value.Humanize(utcDate, dateToCompareAgainst);
                }
                catch (ArgumentException)
                {
                    // See https://github.com/smartstore/Smartstore/issues/1041
                    return value.Humanize(utcDate, dateToCompareAgainst, CultureInfo.InvariantCulture);
                }
            }
        }

        extension(DateTimeOffset value)
        {
            /// <summary>
            /// Epoch time. Number of seconds since midnight (UTC) on 1st January 1970.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public long ToUnixTime()
            {
                return Convert.ToInt64((value.ToUniversalTime() - BeginOfEpoch).TotalSeconds);
            }

            /// <summary>
            /// Converts a DateTimeOffset to a string with native digits
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string ToNativeString(string? format, IFormatProvider? provider = null)
                => FormatWithNativeDigits(value, format, provider);

            /// <summary>
            /// Converts a DateTimeOffset to ISO 8601 string
            /// </summary>
            public string ToIso8601String()
                => value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            /// <inheritdoc cref="DateHumanizeExtensions.Humanize(DateTimeOffset, DateTimeOffset?, CultureInfo)"/>
            public string ToHumanizedString(DateTimeOffset? dateToCompareAgainst = null)
            {
                try
                {
                    return value.Humanize(dateToCompareAgainst);
                }
                catch (ArgumentException)
                {
                    // See https://github.com/smartstore/Smartstore/issues/1041
                    return value.Humanize(dateToCompareAgainst, CultureInfo.InvariantCulture);
                }
            }
        }

        extension(long unixTime)
        {
            /// <summary>
            /// UTC date based on number of seconds since midnight (UTC) on 1st January 1970.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DateTime FromUnixTime()
            {
                return BeginOfEpoch.AddSeconds(unixTime);
            }
        }

        extension(DateOnly value)
        {
            /// <summary>
            /// Converts a DateOnly to a string with native digits
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string ToNativeString(string? format, IFormatProvider? provider = null)
                => FormatWithNativeDigits(value, format, provider);

            /// <summary>
            /// Converts a DateOnly to ISO 8601 string
            /// </summary>
            public string ToIso8601String()
                => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            /// <inheritdoc cref="DateHumanizeExtensions.Humanize(DateOnly, DateOnly?, CultureInfo)"/>
            public string ToHumanizedString(DateOnly? dateToCompareAgainst = null)
            {
                try
                {
                    return value.Humanize(dateToCompareAgainst);
                }
                catch (ArgumentException)
                {
                    // See https://github.com/smartstore/Smartstore/issues/1041
                    return value.Humanize(dateToCompareAgainst, CultureInfo.InvariantCulture);
                }
            }
        }

        extension(TimeOnly value)
        {
            /// <summary>
            /// Converts a TimeOnly to a string with native digits
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string ToNativeString(string? format, IFormatProvider? provider = null)
                => FormatWithNativeDigits(value, format, provider);

            /// <summary>
            /// Converts a TimeOnly to ISO 8601 string
            /// </summary>
            public string ToIso8601String()
                => value.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

            /// <inheritdoc cref="DateHumanizeExtensions.Humanize(TimeOnly, TimeOnly?, bool, CultureInfo)"/>
            public string ToHumanizedString(bool useUtc = true, TimeOnly? dateToCompareAgainst = null)
            {
                try
                {
                    return value.Humanize(dateToCompareAgainst, useUtc);
                }
                catch (ArgumentException)
                {
                    // See https://github.com/smartstore/Smartstore/issues/1041
                    return value.Humanize(dateToCompareAgainst, useUtc, CultureInfo.InvariantCulture);
                }
            }
        }

        extension(TimeSpan value)
        {
            /// <summary>
            /// Converts a TimeSpan to a string with native digits
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string ToNativeString(string? format, IFormatProvider? provider = null)
                => FormatWithNativeDigits(value, format, provider);

            /// <summary>
            /// Converts a TimeSpan to ISO 8601 string
            /// </summary>
            public string ToIso8601String()
                => XmlConvert.ToString(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatWithNativeDigits<T>(T value, string? format, IFormatProvider? provider) where T : IFormattable
        {
            provider ??= CultureInfo.CurrentCulture;
            return value.ToString(format, provider).ReplaceNativeDigits(provider);
        }
    }
}
