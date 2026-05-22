using System.Collections.Concurrent;
using Autofac;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Utilities;

namespace Smartstore.Core.Logging.Serilog;

internal sealed class SmartDbContextSink : IBatchedLogEventSink
{
    // Aggregation window: identical-fingerprint events within this period are collapsed into one record.
    private static readonly TimeSpan AggregationWindow = TimeSpan.FromMinutes(10);

    // Maximum number of additional timestamps stored in the JSON column per record.
    private const int MaxOccurrencesPerEntry = 500;

    // How often we purge expired entries from the in-memory map.
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    private static readonly ConcurrentDictionary<long, AggregationEntry> _aggregationMap = new();

    private static DateTime _lastCleanup = DateTime.MinValue;

    private readonly IFormatProvider _formatProvider;

    public SmartDbContextSink(IFormatProvider formatProvider = null)
    {
        _formatProvider = formatProvider;
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var db = CreateDbContext();
        if (db == null)
        {
            return;
        }

        await using (db)
        {
            db.MinHookImportance = HookImportance.Important;

            var now = DateTime.UtcNow;
            var groups = batch
                .GroupBy(ComputeFingerprint)
                .ToList();

            var toInsert = new List<(long Fingerprint, Log Log, AggregationEntry Entry)>(groups.Count);
            var toUpdate = new List<AggregationEntry>();

            foreach (var group in groups)
            {
                var fingerprint = group.Key;
                var timestamps = group
                    .Select(e => e.Timestamp.UtcDateTime)
                    .Order()
                    .ToList();

                if (_aggregationMap.TryGetValue(fingerprint, out var entry)
                    && now - entry.WindowStart < AggregationWindow)
                {
                    // Existing window: accumulate into the in-memory entry and schedule a DB update.
                    entry.AddOccurrences(timestamps);
                    toUpdate.Add(entry);
                }
                else
                {
                    // New window: create a fresh log record from the earliest event in the group.
                    var firstEvent = group.MinBy(e => e.Timestamp);
                    var log = ConvertLogEvent(firstEvent);

                    var newEntry = new AggregationEntry(timestamps[0]);

                    // Remaining events in the same batch share the same fingerprint → fold them in.
                    if (timestamps.Count > 1)
                    {
                        newEntry.AddOccurrences(timestamps.Skip(1));
                    }

                    // Write aggregated state directly onto the entity before INSERT.
                    var snapshot = newEntry.GetSnapshot();
                    log.OccurrenceCount = snapshot.TotalCount;
                    if (snapshot.Occurrences.Count > 0)
                    {
                        log.Occurrences = snapshot.Occurrences;
                    }

                    toInsert.Add((fingerprint, log, newEntry));
                }
            }

            // INSERT new records and register them in the map.
            if (toInsert.Count > 0)
            {
                db.Logs.AddRange(toInsert.Select(x => x.Log));
                await db.SaveChangesAsync();

                foreach (var (fingerprint, log, entry) in toInsert)
                {
                    entry.LogId = log.Id;
                    _aggregationMap[fingerprint] = entry;
                }
            }

            // UPDATE existing records with the accumulated occurrences.
            // Uses EF Core change tracking so that the JSON-owned collection is serialized correctly.
            if (toUpdate.Count > 0)
            {
                var updateMap = toUpdate.ToDictionary(e => e.LogId);
                var logs = await db.Logs
                    .Where(l => updateMap.Keys.Contains(l.Id))
                    .ToListAsync();

                foreach (var log in logs)
                {
                    var entry = updateMap[log.Id];
                    var snapshot = entry.GetSnapshot();
                    log.OccurrenceCount = snapshot.TotalCount;
                    // Pass a snapshot so future additions to the entry do not affect the tracked instance.
                    log.Occurrences = snapshot.Occurrences.Count > 0 ? snapshot.Occurrences : null;
                }

                await db.SaveChangesAsync();
            }

            // Periodically evict expired entries to keep the map from growing unboundedly.
            if (now - _lastCleanup > CleanupInterval)
            {
                _lastCleanup = now;
                foreach (var key in _aggregationMap.Keys)
                {
                    if (_aggregationMap.TryGetValue(key, out var e)
                        && now - e.WindowStart >= AggregationWindow)
                    {
                        _aggregationMap.TryRemove(key, out _);
                    }
                }
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

    /// <summary>
    /// Builds a stable hash key that identifies semantically equivalent log events.
    /// Two events are considered equivalent when they share the same logger, level,
    /// short message prefix (up to 200 chars), full message, and originating IP address.
    /// </summary>
    private long ComputeFingerprint(LogEvent e)
    {
        var message = e.RenderMessage(_formatProvider);
        if (message?.Length > 200)
        {
            message = message[..200];
        }

        var hash = HashCodeCombiner.Start()
            .Add(e.GetSourceContext())
            .Add((int)e.Level)
            .Add(message)
            .Add(e.Exception?.ToString())
            .Add(e.GetPropertyValue<string>("Ip"));

        return hash.CombinedHashL;
    }

    private Log ConvertLogEvent(LogEvent e)
    {
        var shortMessage = e.RenderMessage(_formatProvider);
        if (shortMessage?.Length > 4000)
        {
            shortMessage = shortMessage.Truncate(4000);
        }

        var log = new Log
        {
            LogLevelId = e.Level == LogEventLevel.Verbose ? 0 : (int)e.Level * 10,
            ShortMessage = shortMessage.Truncate(4000),
            FullMessage = e.Exception?.ToString(),
            CreatedOnUtc = e.Timestamp.UtcDateTime,
            Logger = e.GetSourceContext() ?? "Unknown", // TODO: "Unknown" or "Smartstore"??
            IpAddress = e.GetPropertyValue<string>("Ip"),
            CustomerId = e.GetPropertyValue<int?>("CustomerId"),
            PageUrl = e.GetPropertyValue<string>("Url"),
            ReferrerUrl = e.GetPropertyValue<string>("Referrer"),
            HttpMethod = e.GetPropertyValue<string>("HttpMethod"),
            UserName = e.GetPropertyValue<string>("UserName").Truncate(100),
            UserAgent = e.GetPropertyValue<string>("UserAgent").Truncate(450)
        };

        return log;
    }

    /// <summary>
    /// Tracks the in-memory state for a single aggregation window.
    /// Occurrences holds every timestamp AFTER the first occurrence;
    /// the first is already persisted in <see cref="Log.CreatedOnUtc"/>.
    /// </summary>
    private sealed class AggregationEntry
    {
        private readonly Lock _lock = new();
        private readonly List<LogOccurrence> _occurrences = [];

        public AggregationEntry(DateTime windowStart)
        {
            WindowStart = windowStart;
        }

        public int LogId;
        public DateTime WindowStart { get; }

        /// <summary>Total occurrence count including the first insertion.</summary>
        public int TotalCount { get; private set; } = 1;

        public void AddOccurrences(IEnumerable<DateTime> timestamps)
        {
            lock (_lock)
            {
                foreach (var ts in timestamps)
                {
                    TotalCount++;
                    if (_occurrences.Count < MaxOccurrencesPerEntry)
                    {
                        _occurrences.Add(new LogOccurrence { TimestampUtc = ts });
                    }
                }
            }
        }

        public (int TotalCount, List<LogOccurrence> Occurrences) GetSnapshot()
        {
            lock (_lock)
            {
                return (TotalCount, [.. _occurrences]);
            }
        }
    }
}