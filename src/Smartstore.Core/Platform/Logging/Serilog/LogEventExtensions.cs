using Serilog.Events;
using Smartstore.Core.Logging.Serilog;

namespace Smartstore
{
    public static class LogEventExtensions
    {
        public static T GetPropertyValue<T>(this LogEvent logEvent, string name)
        {
            if (logEvent.Properties.TryGetValue(name, out var value))
            {
                if (value is ScalarValue scalarValue)
                {
                    return scalarValue.Value.Convert<T>();
                }
                else if (value is DelegateScalarValue delegateValue)
                {
                    return delegateValue.Value.Convert<T>();
                }
            }

            return default;
        }

        public static string GetSourceContext(this LogEvent logEvent)
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out var value) && value is ScalarValue scalarValue)
            {
                return (string)scalarValue.Value;
            }

            return null;
        }
    }
}
