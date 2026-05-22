#nullable enable

using System.Threading.RateLimiting;
using Smartstore.Core.Security;

namespace Smartstore.Core.Messaging;

public class QueuedEmailRateLimiter : Disposable, IEmailRateLimiter
{
    private readonly TokenBucketRateLimiter? _sendRateLimiter;
    private readonly RateLimiter _logRateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
    {
        QueueLimit = 1,
        ReplenishmentPeriod = TimeSpan.FromSeconds(5),
        TokensPerPeriod = 20,
        TokenLimit = 20
    });

    public QueuedEmailRateLimiter(ResiliencySettings settings)
    {
        _sendRateLimiter = CreateTokenBucket(settings.MailSendRateLimit, settings.MailSendRateWindow);
    }

    public ILogger Logger { get; } = NullLogger.Instance;

    public virtual int GetAllowedSendCount(int requestedCount)
    {
        CheckDisposed();

        if (requestedCount <= 0)
        {
            return 0;
        }

        if (_sendRateLimiter == null)
        {
            return requestedCount;
        }

        for (var allowedCount = requestedCount; allowedCount > 0; allowedCount--)
        {
            using var lease = _sendRateLimiter.AttemptAcquire(allowedCount);
            if (lease.IsAcquired)
            {
                if (allowedCount < requestedCount)
                {
                    TryLogThrottled($"Queued mail rate limit partially applied. Requested: {requestedCount}, Granted: {allowedCount}.");
                }

                return allowedCount;
            }
        }

        TryLogThrottled($"Queued mail rate limit exceeded. Requested: {requestedCount}, Granted: 0.");
        return 0;
    }

    protected override void OnDispose(bool disposing)
    {
        if (disposing)
        {
            _sendRateLimiter?.Dispose();
            _logRateLimiter?.Dispose();
        }
    }

    private void TryLogThrottled(string message)
    {
        using var logLease = _logRateLimiter.AttemptAcquire();
        if (logLease.IsAcquired)
        {
            Logger.Warn(message);
        }
    }

    private static int? NormalizeLimit(int? limit)
        => limit > 0 ? limit : null;

    private static TokenBucketRateLimiter? CreateTokenBucket(int? limit, TimeSpan period)
    {
        limit = NormalizeLimit(limit);
        if (limit == null || period <= TimeSpan.Zero)
        {
            return null;
        }

        return new TokenBucketRateLimiter(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = limit.Value,
                TokensPerPeriod = limit.Value,
                ReplenishmentPeriod = period,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    }
}