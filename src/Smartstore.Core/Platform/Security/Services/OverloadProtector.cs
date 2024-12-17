using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Smartstore.Caching;

namespace Smartstore.Core.Security
{
    public class OverloadProtector : IOverloadProtector
    {
        private readonly ResiliencySettings _settings;
        private readonly ICacheManager _cache;

        // Type-specific limiters
        private RateLimiter _guestLongLimiter;
        private RateLimiter _guestPeakLimiter;

        private RateLimiter _customerLongLimiter;
        private RateLimiter _customerPeakLimiter;

        private RateLimiter _botLongLimiter;
        private RateLimiter _botPeakLimiter;

        // Global limiters that are more generous
        private RateLimiter _globalLongLimiter;
        private RateLimiter _globalPeakLimiter;

        public OverloadProtector(ResiliencySettings settings)
        {
            _settings = settings;

            // Create type-specific limiters
            _guestLongLimiter = CreateTokenBucket(settings.LongTrafficLimitGuest, settings.LongTrafficWindow);
            _customerLongLimiter = CreateTokenBucket(settings.LongTrafficLimitCustomer, settings.LongTrafficWindow);
            _botLongLimiter = CreateTokenBucket(settings.LongTrafficLimitBot, settings.LongTrafficWindow);

            _guestPeakLimiter = CreateTokenBucket(settings.PeakTrafficLimitGuest, settings.PeakTrafficWindow);
            _customerPeakLimiter = CreateTokenBucket(settings.PeakTrafficLimitCustomer, settings.PeakTrafficWindow);
            _botPeakLimiter = CreateTokenBucket(settings.PeakTrafficLimitBot, settings.PeakTrafficWindow);

            // Create global limiters with more generous limits
            _globalLongLimiter = CreateTokenBucket(settings.LongTrafficLimitGlobal, settings.LongTrafficWindow);
            _globalPeakLimiter = CreateTokenBucket(settings.PeakTrafficLimitGlobal, settings.PeakTrafficWindow);
        }

        public Task<bool> DenyGuestAsync()
            => Task.FromResult(CheckDeny(UserType.Guest));

        public Task<bool> DenyCustomerAsync()
            => Task.FromResult(CheckDeny(UserType.Customer));

        public Task<bool> DenyBotAsync()
            => Task.FromResult(CheckDeny(UserType.Bot));

        public Task<bool> ForbidNewGuestAsync(HttpContext httpContext)
        {
            var forbid = _settings.EnableOverloadProtection && _settings.ForbidNewGuestsIfAjaxOrPost && httpContext != null;
            if (forbid)
            {
                forbid = !httpContext.Request.IsNonAjaxGet();
            }
            
            return Task.FromResult(forbid);
        }

        private static TokenBucketRateLimiter CreateTokenBucket(int? limit, TimeSpan period)
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

        private bool CheckDeny(UserType userType)
        {
            if (!_settings.EnableOverloadProtection)
            {
                // Allowed, because protection is turned off.
                return false;
            }

            // First check type-specific peak limit
            if (!TryAcquireFromType(userType, peak: true))
            {
                // If category peak fails, try global peak
                if (!TryAcquireFromGlobal(peak: true))
                {
                    // If even global peak fails (if ever), deny
                    return true;
                }
            }

            // Now check type-specific long window
            if (!TryAcquireFromType(userType, peak: false))
            {
                // If type long fails, try global long
                if (!TryAcquireFromGlobal(peak: false))
                {
                    // If even global long fails (should never happen with huge config), deny
                    return true;
                }
            }

            // If we got here, either type or global allowed it
            return false; // no deny (allowed)
        }

        private bool TryAcquireFromType(UserType userType, bool peak)
        {
            var limiter = GetTypeLimiter(userType, peak);
            if (limiter != null)
            {
                var lease = limiter.AttemptAcquire(1);
                return lease.IsAcquired;
            }

            // Always allow access if no rate limiting is configured
            return true;
        }

        private bool TryAcquireFromGlobal(bool peak)
        {
            var limiter = peak ? _globalPeakLimiter : _globalLongLimiter;
            if (limiter != null)
            {
                var lease = limiter.AttemptAcquire(1);
                return lease.IsAcquired;
            }

            // Always allow access if no rate limiting is configured
            return true;
        }

        private RateLimiter GetTypeLimiter(UserType userType, bool peak)
        {
            return userType switch
            {
                UserType.Customer   => peak ? _customerPeakLimiter : _customerLongLimiter,
                UserType.Guest      => peak ? _guestPeakLimiter : _guestLongLimiter,
                _                   => peak ? _botPeakLimiter : _botLongLimiter
            };
        }

        enum UserType
        {
            Customer,
            Guest,
            Bot
        }
    }
}
