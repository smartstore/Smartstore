using Autofac;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Logging.Serilog
{
    internal sealed class SmartDbContextSink : IBatchedLogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public SmartDbContextSink(IFormatProvider formatProvider = null)
        {
            _formatProvider = formatProvider;
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            var db = CreateDbContext();

            if (db != null)
            {
                await using (db)
                {
                    db.MinHookImportance = HookImportance.Important;
                    db.Logs.AddRange(batch.Select(CovertLogEvent));
                    await db.SaveChangesAsync();
                }
            }
        }

        public Task OnEmptyBatchAsync()
            => Task.CompletedTask;

        private static SmartDbContext CreateDbContext()
        {
            var engine = EngineContext.Current;

            if (engine == null || !engine.IsStarted)
            {
                // App not initialized yet
                return null;
            }

            if (!DataSettings.DatabaseIsInstalled())
            {
                // Cannot log to non-existent database
                return null;
            }

            return engine.Application.Services.Resolve<IDbContextFactory<SmartDbContext>>().CreateDbContext();
        }

        private Log CovertLogEvent(LogEvent e)
        {
            var shortMessage = e.RenderMessage(_formatProvider);
            if (shortMessage?.Length > 4000)
            {
                shortMessage = shortMessage.Truncate(4000);
            }

            var log = new Log
            {
                LogLevelId = e.Level == LogEventLevel.Verbose ? 0 : (int)e.Level * 10,
                ShortMessage = shortMessage,
                FullMessage = e.Exception?.ToString(),
                CreatedOnUtc = e.Timestamp.UtcDateTime,
                Logger = e.GetSourceContext() ?? "Unknown", // TODO: "Unknown" or "Smartstore"??
                IpAddress = e.GetPropertyValue<string>("Ip"),
                CustomerId = e.GetPropertyValue<int?>("CustomerId"),
                PageUrl = e.GetPropertyValue<string>("Url"),
                ReferrerUrl = e.GetPropertyValue<string>("Referrer"),
                HttpMethod = e.GetPropertyValue<string>("HttpMethod"),
                UserName = e.GetPropertyValue<string>("UserName")
            };

            return log;
        }
    }
}
