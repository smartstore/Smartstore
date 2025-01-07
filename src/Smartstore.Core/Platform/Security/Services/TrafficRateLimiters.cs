#nullable enable

using System.Threading.RateLimiting;
using Microsoft.Extensions.Hosting;

namespace Smartstore.Core.Security
{
    public class TrafficRateLimiters(ResiliencySettings settings) : Disposable, IHostedService
    {
        // Long limiters
        public RateLimiter? LongGuestLimiter { get; private set; } = CreateTokenBucket(settings.LongTrafficLimitGuest, settings.LongTrafficWindow);
        public RateLimiter? LongBotLimiter { get; private set; } = CreateTokenBucket(settings.LongTrafficLimitBot, settings.LongTrafficWindow);
        public RateLimiter? LongGlobalLimiter { get; private set; } = CreateTokenBucket(settings.LongTrafficLimitGlobal, settings.LongTrafficWindow);

        // Peak limiters
        public RateLimiter? PeakGuestLimiter { get; private set; } = CreateTokenBucket(settings.PeakTrafficLimitGuest, settings.PeakTrafficWindow);
        public RateLimiter? PeakBotLimiter { get; private set; } = CreateTokenBucket(settings.PeakTrafficLimitBot, settings.PeakTrafficWindow);
        public RateLimiter? PeakGlobalLimiter { get; private set; } = CreateTokenBucket(settings.PeakTrafficLimitGlobal, settings.PeakTrafficWindow);

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

        public Task StartAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Dispose();
            }
            catch
            {
                // Ignore exceptions thrown as a result of a cancellation.
            }   

            return Task.CompletedTask;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                DisposeInternal();
            }
        }

        private void DisposeInternal()
        {
            LongGuestLimiter?.Dispose();
            LongGuestLimiter = null;
            LongBotLimiter?.Dispose();
            LongBotLimiter = null;
            LongGlobalLimiter?.Dispose();
            LongGlobalLimiter = null;
            PeakGuestLimiter?.Dispose();
            PeakGuestLimiter = null;
            PeakBotLimiter?.Dispose();
            PeakBotLimiter = null;
            PeakGlobalLimiter?.Dispose();
            PeakGlobalLimiter = null;
        }
    }
}
