#nullable enable

using System.Threading.RateLimiting;

namespace Smartstore.Core.Security
{
    public class TrafficRateLimiters(ResiliencySettings settings)
    {
        // Type-specific limiters
        public RateLimiter? GuestLongLimiter { get; } = CreateTokenBucket(settings.LongTrafficLimitGuest, settings.LongTrafficWindow);
        public RateLimiter? GuestPeakLimiter { get; } = CreateTokenBucket(settings.PeakTrafficLimitGuest, settings.PeakTrafficWindow);

        public RateLimiter? BotLongLimiter { get; } = CreateTokenBucket(settings.LongTrafficLimitBot, settings.LongTrafficWindow);
        public RateLimiter? BotPeakLimiter { get; } = CreateTokenBucket(settings.PeakTrafficLimitBot, settings.PeakTrafficWindow);

        // Global limiters that are more generous
        public RateLimiter? GlobalLongLimiter { get; } = CreateTokenBucket(settings.LongTrafficLimitGlobal, settings.LongTrafficWindow);
        public RateLimiter? GlobalPeakLimiter { get; } = CreateTokenBucket(settings.PeakTrafficLimitGlobal, settings.PeakTrafficWindow);

        private static TokenBucketRateLimiter? CreateTokenBucket(int? limit, TimeSpan period)
        {
            if (limit == null || limit <= 0)
            {
                // No rate limiting (unlimited access)
                return null;
            }

            // Creates a TokenBucket with ‘limit’ tokens per ‘period’.
            // The quota is completely refilled every period.
            return new TokenBucketRateLimiter(
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = limit.Value,
                    TokensPerPeriod = limit.Value,
                    ReplenishmentPeriod = period,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0 // No queue
                });
        }
    }
}
