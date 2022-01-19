using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Smartstore.Utilities
{
    public static class Throttle
    {
        private readonly static ConcurrentDictionary<string, CheckEntry> _checks = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Performs a throttled check.
        /// </summary>
        /// <param name="key">Identifier for the check process</param>
        /// <param name="interval">Interval between actual checks</param>
        /// <param name="check">The check factory</param>
        /// <returns>Check result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(string key, TimeSpan interval, Func<bool> check)
        {
            return CheckAsync(key, interval, false, () => Task.FromResult(check())).Await();
        }

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
            return CheckAsync(key, interval, recheckWhenFalse, () => Task.FromResult(check())).Await();
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
        {
            return CheckAsync(key, interval, false, check);
        }

        /// <summary>
        /// Performs a throttled check.
        /// </summary>
        /// <param name="key">Identifier for the check process</param>
        /// <param name="interval">Interval between actual checks</param>
        /// <param name="recheckWhenFalse"></param>
        /// <param name="check">The check factory</param>
        /// <returns>Check result</returns>
        public static async Task<bool> CheckAsync(string key, TimeSpan interval, bool recheckWhenFalse, Func<Task<bool>> check)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(check, nameof(check));

            bool added = false;
            var now = DateTime.UtcNow;

            if (!_checks.TryGetValue(key, out var entry))
            {
                added = true;
                entry = new CheckEntry { Value = await check(), NextCheckUtc = (now + interval) };
                _checks.TryAdd(key, entry);
            }

            var ok = entry.Value;

            if (added)
            {
                return ok;
            }

            var isOverdue = (!ok && recheckWhenFalse) || (now > entry.NextCheckUtc);

            if (isOverdue)
            {
                // Check is overdue: recheck
                ok = await check();
                _checks.TryUpdate(key, new CheckEntry { Value = ok, NextCheckUtc = (now + interval) }, entry);
            }

            return ok;
        }

        class CheckEntry
        {
            public bool Value { get; set; }
            public DateTime NextCheckUtc { get; set; }
        }
    }
}
