using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Identity;
using Smartstore.Core.Web;

namespace Smartstore.Core.Security
{
    public class OverloadProtector : IOverloadProtector
    {
        private readonly Work<ResiliencySettings> _settings;
        private readonly Lazy<TrafficRateLimiters> _rateLimiters;

        private readonly RateLimiter _logRateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            QueueLimit = 1,
            ReplenishmentPeriod = TimeSpan.FromSeconds(5),
            TokensPerPeriod = 100,
            TokenLimit = 200
        });

        public OverloadProtector(
            Work<ResiliencySettings> settings,
            Lazy<TrafficRateLimiters> rateLimiters,
            ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _rateLimiters = rateLimiters;

            Logger = loggerFactory.CreateLogger("File/App_Data/Logs/overloadprotector-.log");
        }

        public ILogger Logger { get; }

        public virtual Task<bool> DenyGuestAsync(HttpContext httpContext, Customer customer = null)
            => Task.FromResult(CheckDeny(UserType.Guest));

        public virtual Task<bool> DenyBotAsync(HttpContext httpContext, IUserAgent userAgent)
            => Task.FromResult(CheckDeny(UserType.Bot));

        public virtual Task<bool> ForbidNewGuestAsync(HttpContext httpContext)
        {
            var forbid = _settings.Value.EnableOverloadProtection && _settings.Value.ForbidNewGuestsIfSubRequest && httpContext != null;
            if (forbid)
            {
                forbid = httpContext.Request.IsSubRequest();
                if (forbid)
                {
                    TryLogThrottled(httpContext, "Sub-request blocked due to overload protection policy.");
                }
            }
            
            return Task.FromResult(forbid);
        }

        private bool CheckDeny(UserType userType)
        {
            if (!_settings.Value.EnableOverloadProtection)
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
                using var lease = limiter.AttemptAcquire(1);

                if (!lease.IsAcquired)
                {
                    Logger.Warn("Rate limit exceeded. UserType: {0}, Peak: {1}", userType, peak);
                }

                return lease.IsAcquired;
            }

            // Always allow access if no rate limiting is configured
            return true;
        }

        private bool TryAcquireFromGlobal(bool peak)
        {
            var limiter = peak ? _rateLimiters.Value.PeakGlobalLimiter : _rateLimiters.Value.LongGlobalLimiter;
            if (limiter != null)
            {
                using var lease = limiter.AttemptAcquire(1);

                if (!lease.IsAcquired)
                {
                    Logger.Warn("Global rate limit exceeded. Peak: {0}", peak);
                }

                return lease.IsAcquired;
            }

            // Always allow access if no rate limiting is configured
            return true;
        }

        private RateLimiter GetTypeLimiter(UserType userType, bool peak)
        {
            return userType switch
            {
                UserType.Guest  => peak ? _rateLimiters.Value.PeakGuestLimiter : _rateLimiters.Value.LongGuestLimiter,
                _               => peak ? _rateLimiters.Value.PeakBotLimiter : _rateLimiters.Value.LongBotLimiter
            };
        }

        private void TryLogThrottled(HttpContext httpContext, string message)
        {
            using var logLease = _logRateLimiter.AttemptAcquire();
            if (logLease.IsAcquired)
            {
                if (httpContext != null)
                {
                    var webHelper = httpContext.RequestServices.GetRequiredService<IWebHelper>();
                    var ipAddress = webHelper.GetClientIpAddress().ToString();

                    message += $" IP: {ipAddress}, Path: {httpContext.Request.Path}.";
                }

                Logger.Warn(message);
            }       
        }

        enum UserType
        {
            Guest,
            Bot
        }
    }
}
