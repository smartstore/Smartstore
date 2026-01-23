using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Smartstore.Utilities;

public static class Throttle
{
    private static readonly ConcurrentDictionary<string, CheckEntry> _checks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Performs a throttled check.
    /// </summary>
    /// <param name="key">Identifier for the check process</param>
    /// <param name="interval">Interval between actual checks</param>
    /// <param name="check">The check factory</param>
    /// <returns>Check result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Check(string key, TimeSpan interval, Func<bool> check)
        => Check(key, interval, recheckWhenFalse: false, check);

    /// <summary>
    /// Performs a throttled check.
    /// </summary>
    /// <param name="key">Identifier for the check process</param>
    /// <param name="interval">Interval between actual checks</param>
    /// <param name="recheckWhenFalse"></param>
    /// <param name="check">The check factory</param>
    /// <returns>Check result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Check(string key, TimeSpan interval, bool recheckWhenFalse, Func<bool> check)
    {
        Guard.NotEmpty(key, nameof(key));
        Guard.NotNull(check, nameof(check));

        // Fast path: avoid async/state machine + avoid Task allocation.
        var now = DateTime.UtcNow;

        if (!_checks.TryGetValue(key, out var entry))
        {
            var ok = check();
            // Another thread may race; we accept overwriting only via TryAdd failure,
            // in which case we return our computed result (same behavior as before: "best effort" cache).
            _checks.TryAdd(key, new CheckEntry(ok, now + interval));
            return ok;
        }

        var value = entry.Value;

        // Only re-check if overdue or if caller wants to recheck failed results.
        if ((value && now <= entry.NextCheckUtc) || (!value && !recheckWhenFalse && now <= entry.NextCheckUtc))
        {
            return value;
        }

        var newValue = check();
        _checks.TryUpdate(key, new CheckEntry(newValue, now + interval), entry);
        return newValue;
    }

    /// <summary>
    /// Performs a throttled check.
    /// </summary>
    /// <param name="key">Identifier for the check process</param>
    /// <param name="interval">Interval between actual checks</param>
    /// <param name="check">The check factory</param>
    /// <returns>Check result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<bool> CheckAsync(string key, TimeSpan interval, Func<Task<bool>> check)
        => CheckAsync(key, interval, recheckWhenFalse: false, check);

    /// <summary>
    /// Performs a throttled check.
    /// </summary>
    /// <param name="key">Identifier for the check process</param>
    /// <param name="interval">Interval between actual checks</param>
    /// <param name="recheckWhenFalse"></param>
    /// <param name="check">The check factory</param>
    /// <returns>Check result</returns>
    public static Task<bool> CheckAsync(string key, TimeSpan interval, bool recheckWhenFalse, Func<Task<bool>> check)
    {
        Guard.NotEmpty(key, nameof(key));
        Guard.NotNull(check, nameof(check));

        var now = DateTime.UtcNow;

        if (!_checks.TryGetValue(key, out var entry))
        {
            return AddAndReturnAsync(key, now, interval, check);
        }

        var value = entry.Value;

        // Not overdue: return cached result without async/state machine.
        if (!((!value && recheckWhenFalse) || now > entry.NextCheckUtc))
        {
            return Task.FromResult(value);
        }

        return RecheckAndUpdateAsync(key, entry, now, interval, check);
    }

    private static async Task<bool> AddAndReturnAsync(string key, DateTime now, TimeSpan interval, Func<Task<bool>> check)
    {
        var ok = await check().ConfigureAwait(false);
        _checks.TryAdd(key, new CheckEntry(ok, now + interval));
        return ok;
    }

    private static async Task<bool> RecheckAndUpdateAsync(string key, CheckEntry entry, DateTime now, TimeSpan interval, Func<Task<bool>> check)
    {
        var ok = await check().ConfigureAwait(false);
        _checks.TryUpdate(key, new CheckEntry(ok, now + interval), entry);
        return ok;
    }

    private readonly record struct CheckEntry(bool Value, DateTime NextCheckUtc);
}
