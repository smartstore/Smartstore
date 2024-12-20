using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Identity;
using Smartstore.Core.Web;

namespace Smartstore.Core.Security
{
    public class OverloadProtector : IOverloadProtector
    {
        private readonly ResiliencySettings _settings;
        private readonly TrafficRateLimiters _rateLimiters;

        public OverloadProtector(ResiliencySettings settings, TrafficRateLimiters rateLimiters)
        {
            _settings = settings;
            _rateLimiters = rateLimiters;
        }

        public virtual Task<bool> DenyGuestAsync(Customer customer = null)
            => Task.FromResult(CheckDeny(UserType.Guest));

        public virtual Task<bool> DenyBotAsync(IUserAgent userAgent)
            => Task.FromResult(CheckDeny(UserType.Bot));

        public virtual Task<bool> ForbidNewGuestAsync(HttpContext httpContext)
        {
            var forbid = _settings.EnableOverloadProtection && _settings.ForbidNewGuestsIfSubRequest && httpContext != null;
            if (forbid)
            {
                forbid = httpContext.Request.IsSubRequest();
            }
            
            return Task.FromResult(forbid);
        }

        private bool CheckDeny(UserType userType)
        {
            if (!_settings.EnableOverloadProtection)
            {
                // Allowed, because protection is turned off.
                return false;
            }

            // Check both global and type-specific limits for peak usage
            var peakAllowed = TryAcquireFromGlobal(peak: true) && TryAcquireFromType(userType, peak: true);
            if (!peakAllowed)
            {
                // Deny the request if either limit fails
                return true;
            }

            // Check both global and type-specific limits for long usage
            var longAllowed = TryAcquireFromGlobal(peak: false) && TryAcquireFromType(userType, peak: false);
            if (!longAllowed)
            {
                // Deny the request if either limit fails
                return true;
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
            var limiter = peak ? _rateLimiters.GlobalPeakLimiter : _rateLimiters.GlobalLongLimiter;
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
                UserType.Guest  => peak ? _rateLimiters.GuestPeakLimiter : _rateLimiters.GuestLongLimiter,
                _               => peak ? _rateLimiters.BotPeakLimiter : _rateLimiters.BotLongLimiter
            };
        }

        enum UserType
        {
            Guest,
            Bot
        }
    }
}
