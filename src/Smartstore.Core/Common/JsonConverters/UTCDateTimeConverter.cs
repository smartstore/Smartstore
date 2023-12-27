using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Smartstore.Core.Common.Services;

namespace Smartstore.Core.Common.JsonConverters
{
    public class UTCDateTimeConverter : DateTimeConverterBase
    {
        private readonly DateTimeConverterBase _innerConverter;

        public UTCDateTimeConverter(DateTimeConverterBase innerConverter)
        {
            _innerConverter = Guard.NotNull(innerConverter, nameof(innerConverter));
        }

        public override bool CanConvert(Type objectType)
        {
            return _innerConverter.CanConvert(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return _innerConverter.ReadJson(reader, objectType, existingValue, serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTime d)
            {
                if (d.Kind == DateTimeKind.Unspecified)
                {
                    // when DateTime kind is "Unspecified", it was very likely converted from UTC to 
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
                        value = dtHelper.ConvertToUtcTime(d, dtHelper.CurrentTimeZone);
                    }
                }

                // In some calandars default DateTime Min/Max values are not supported, fix it.
                var calendar = CultureInfo.CurrentCulture.Calendar;

                if (d < calendar.MinSupportedDateTime)
                {
                    value = calendar.MinSupportedDateTime;
                }

                if (d > calendar.MaxSupportedDateTime)
                {
                    value = calendar.MaxSupportedDateTime;
                }
            }

            _innerConverter.WriteJson(writer, value, serializer);
        }
    }
}
