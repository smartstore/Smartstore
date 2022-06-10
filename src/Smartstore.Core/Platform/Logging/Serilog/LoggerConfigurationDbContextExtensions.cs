using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;

namespace Smartstore.Core.Logging.Serilog
{
    public static class LoggerConfigurationDbContextExtensions
    {
        public static LoggerConfiguration DbContext(this LoggerSinkConfiguration loggerConfiguration,
            TimeSpan? period = null,
            int batchSize = 100,
            bool eagerlyEmitFirstEvent = true,
            int? queueLimit = 1000,
            IFormatProvider formatProvider = null)
        {
            var sink = new SmartDbContextSink(formatProvider);

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = batchSize,
                Period = period ?? TimeSpan.FromSeconds(5),
                EagerlyEmitFirstEvent = eagerlyEmitFirstEvent,
                QueueLimit = queueLimit
            };

            var batchingSink = new PeriodicBatchingSink(sink, batchingOptions);

            return loggerConfiguration.Sink(batchingSink);
        }
    }
}
