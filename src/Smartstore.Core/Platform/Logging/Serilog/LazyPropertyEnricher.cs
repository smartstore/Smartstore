using Serilog.Core;
using Serilog.Events;

namespace Smartstore.Core.Logging.Serilog
{
    internal class LazyPropertyEnricher : ILogEventEnricher
    {
        readonly string _name;
        readonly object _value;
        readonly bool _destructureObjects;

        public LazyPropertyEnricher(string name, object value, bool destructureObjects = false)
        {
            Guard.NotNull(name, nameof(name));

            _name = name;
            _value = value;
            _destructureObjects = destructureObjects;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var property = propertyFactory.CreateProperty(_name, _value, _destructureObjects);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
