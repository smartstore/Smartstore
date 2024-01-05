using System.Reflection;
using Serilog.Core;
using Serilog.Events;
using Smartstore.ComponentModel;

namespace Smartstore.Core.Logging.Serilog
{
    internal class DelegatingPropertyEnricher : ILogEventEnricher
    {
        readonly string _name;
        readonly DelegateScalarValue _value;

        public DelegatingPropertyEnricher(string name, Func<object> valueAccessor)
        {
            Guard.NotNull(name);
            Guard.NotNull(valueAccessor);

            _name = name;
            _value = new DelegateScalarValue(valueAccessor);
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var property = new LogEventProperty(_name, _value);
            logEvent.AddPropertyIfAbsent(property);
        }

        public void FreezeValue()
            => _value.Freeze();
    }

    internal class DelegateScalarValue : LogEventPropertyValue
    {
        // Get internal method
        //  --> internal static void Render(object value, TextWriter output, string format = null, IFormatProvider formatProvider = null)
        static readonly FastInvoker ScalarValueRenderMethodInvoker = new(typeof(ScalarValue).GetMethod(
            "Render",
            BindingFlags.Static | BindingFlags.NonPublic,
            new[] { typeof(object), typeof(TextWriter), typeof(string), typeof(IFormatProvider) }));

        Func<object> _valueAccessor;
        object _value;
        bool _freezed;

        public DelegateScalarValue(Func<object> valueAccessor)
        {
            _valueAccessor = Guard.NotNull(valueAccessor);
        }

        public override void Render(TextWriter output, string format = null, IFormatProvider formatProvider = null)
        {
            ScalarValueRenderMethodInvoker.Invoke(null, Value, output, format, formatProvider);
        }

        public void Freeze()
        {
            if (_valueAccessor != null)
            {
                _value = _valueAccessor();
                _valueAccessor = null;
            }

            _freezed = true;
        }

        public object Value
        {
            get => _freezed ? _value : _valueAccessor?.Invoke();
        }
    }
}
