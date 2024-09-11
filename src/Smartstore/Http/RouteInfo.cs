#nullable enable

using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace Smartstore.Http
{
    [JsonConverter(typeof(RouteInfoConverter))]
    public class RouteInfo
    {
        public RouteInfo(RouteInfo cloneFrom)
            : this(cloneFrom.Action, cloneFrom.Controller, new RouteValueDictionary(cloneFrom.RouteValues))
        {
        }

        public RouteInfo(string action, object routeValues)
            : this(action, null, routeValues)
        {
        }

        public RouteInfo(string action, string? controller, object? routeValues)
            : this(action, controller, new RouteValueDictionary(routeValues))
        {
        }

        public RouteInfo(string action, IDictionary<string, object?> routeValues)
            : this(action, null, routeValues)
        {
        }

        public RouteInfo(string action, string? controller, IDictionary<string, object?> routeValues)
            : this(action, controller, new RouteValueDictionary(routeValues))
        {
            Guard.NotNull(routeValues);
        }

        public RouteInfo(string action, RouteValueDictionary routeValues)
            : this(action, null, routeValues)
        {
        }

        [JsonConstructor]
        public RouteInfo(string action, string? controller, RouteValueDictionary routeValues)
        {
            Guard.NotEmpty(action);
            Guard.NotNull(routeValues);

            Action = action;
            Controller = controller;
            RouteValues = routeValues;
        }

        public string Action { get; }
        public string? Controller { get; }
        public RouteValueDictionary RouteValues { get; }
    }

    #region JsonConverter

    internal class RouteInfoConverter : JsonConverter<RouteInfo>
    {
        public override bool CanWrite
            => false;

        public override RouteInfo? ReadJson(JsonReader reader, Type objectType, RouteInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string? action = null;
            string? controller = null;
            RouteValueDictionary? routeValues = null;

            reader.Read();
            while (reader.TokenType == JsonToken.PropertyName)
            {
                string? a = reader.Value?.ToString();
                if (string.Equals(a, "Action", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    action = serializer.Deserialize<string>(reader);
                }
                else if (string.Equals(a, "Controller", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    controller = serializer.Deserialize<string>(reader);
                }
                else if (string.Equals(a, "RouteValues", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    routeValues = serializer.Deserialize<RouteValueDictionary>(reader);
                }
                else
                {
                    reader.Skip();
                }

                reader.Read();
            }

            var routeInfo = Activator.CreateInstance(objectType, [action, controller, routeValues]);

            return (RouteInfo?)routeInfo;
        }

        public override void WriteJson(JsonWriter writer, RouteInfo? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
