using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Smartstore.Core.Common.Services;

namespace Smartstore.Core.Common.JsonConverters
{
    /// <summary>
    /// The STJ variant of <see cref="UTCDateTimeConverter"/>.
    /// Converts <see cref="DateTime"/> values to and from JSON, ensuring that dates are handled in UTC format during
    /// serialization and deserialization.
    /// </summary>
    public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDateTime();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var d = value;

            if (d.Kind == DateTimeKind.Unspecified)
            {
                // When DateTime kind is "Unspecified", it was very likely converted from UTC to 
                // SERVER*s preferred local time before (with DateTimeHelper.ConvertToUserTime()).
                // While this works fine during server-time rendering, it can lead to wrong UTC offsets
                // on the client (e.g. in AJAX mode Grids, where rendering is performed locally with JSON data).
                // The issue occurs when the client's time zone is not the same as "CurrentTimeZone" (configured in the backend).
                // To fix it, we have to convert the date back to UTC kind, but with the SERVER PREFERRED TIMEZONE
                // in order to calculate with the correct UTC offset. Then it's up to the client to display the date
                // in the CLIENT's time zone. Which is not perfect of course, because the same date would be displayed in the 
                // "CurrentTimeZone" if rendered on server.
                // But: it fixes the issue and is way better than converting all AJAXable dates to strings on the server.
                var dtHelper = EngineContext.Current.Scope.ResolveOptional<IDateTimeHelper>();
                if (dtHelper != null)
                {
                    d = dtHelper.ConvertToUtcTime(d, dtHelper.CurrentTimeZone);
                }
            }

            // In some calandars default DateTime Min/Max values are not supported, fix it.
            var cal = CultureInfo.CurrentCulture.Calendar;

            if (d < cal.MinSupportedDateTime) d = cal.MinSupportedDateTime;
            if (d > cal.MaxSupportedDateTime) d = cal.MaxSupportedDateTime;

            writer.WriteStringValue(d);
        }
    }
}