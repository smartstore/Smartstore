using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Engine;

namespace Smartstore.Core.Logging.Serilog
{
    public sealed class SmartDbContextSink : IBatchedLogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public SmartDbContextSink(IFormatProvider formatProvider = null)
        {
            _formatProvider = formatProvider;
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            var entities = batch.Select(CovertLogEvent);

            var db = CreateDbContext();

            if (db != null)
            {
                using (db)
                {
                    db.Logs.AddRange(entities);
                    await db.SaveChangesAsync();
                }
            }
        }

        public Task OnEmptyBatchAsync()
            => Task.CompletedTask;

        private static SmartDbContext CreateDbContext()
        {
            var engine = EngineContext.Current;

            if (engine == null && !engine.IsStarted)
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
            return new Log
            {
                LogLevelId = (int)ConvertLogLevel(e.Level),
                ShortMessage = e.RenderMessage(_formatProvider),
                FullMessage = e.Exception?.ToString(),
                CreatedOnUtc = e.Timestamp.UtcDateTime,
                Logger = e.GetSourceContext() ?? "Unknown", // TODO: "Unknown" or "Smartstore"??
                IpAddress = e.GetScalarPropertyValue<string>("Ip"),
                CustomerId = e.GetScalarPropertyValue<int?>("CustomerId"),
                PageUrl = e.GetScalarPropertyValue<string>("Url"),
                ReferrerUrl = e.GetScalarPropertyValue<string>("Referrer"),
                HttpMethod = e.GetScalarPropertyValue<string>("HttpMethod"),
                UserName = e.GetScalarPropertyValue<string>("UserName")
            };

            static LogLevel ConvertLogLevel(LogEventLevel level)
            {
                switch (level)
                {
                    case LogEventLevel.Debug:
                        return LogLevel.Debug;
                    case LogEventLevel.Information:
                        return LogLevel.Information;
                    case LogEventLevel.Warning:
                        return LogLevel.Warning;
                    case LogEventLevel.Error:
                        return LogLevel.Error;
                    case LogEventLevel.Fatal:
                        return LogLevel.Fatal;
                    default:
                        return LogLevel.Verbose;
                }
            }
        }
    }
}
