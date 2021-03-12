using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
            return CheckAsync(key, interval, false, () => Task.FromResult(check())).GetAwaiter().GetResult();
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
            return CheckAsync(key, interval, recheckWhenFalse, () => Task.FromResult(check())).GetAwaiter().GetResult();
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

            var entry = _checks.GetOrAdd(key, x =>
            {
                added = true;
                return new CheckEntry { Value = check(), NextCheckUtc = (now + interval) };
            });

            var ok = await entry.Value;

            if (added)
            {
                return ok;
            }

            var isOverdue = (!ok && recheckWhenFalse) || (now > entry.NextCheckUtc);

            if (isOverdue)
            {
                // Check is overdue: recheck
                _checks.TryUpdate(key, new CheckEntry { Value = check(), NextCheckUtc = (now + interval) }, entry);
                ok = await check();
            }

            return ok;
        }

        class CheckEntry
        {
            public Task<bool> Value { get; set; }
            public DateTime NextCheckUtc { get; set; }
        }
    }
}
