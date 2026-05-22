using System;
using NUnit.Framework;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;

namespace Smartstore.Core.Tests.Platform.Messaging;

[TestFixture]
public class QueuedEmailRateLimiterTests
{
    [Test]
    public void GetAllowedMailCount_returns_requested_count_when_rate_limit_is_disabled()
    {
        using var limiter = CreateLimiter(sendRateLimit: null);

        Assert.That(limiter.GetAllowedSendCount(7), Is.EqualTo(7));
    }

    [Test]
    public void GetAllowedMailCount_returns_partial_count_when_only_partial_quota_is_available()
    {
        using var limiter = CreateLimiter(sendRateLimit: 2);

        Assert.That(limiter.GetAllowedSendCount(1), Is.EqualTo(1));
        Assert.That(limiter.GetAllowedSendCount(2), Is.EqualTo(1));
        Assert.That(limiter.GetAllowedSendCount(1), Is.EqualTo(0));
    }

    private static QueuedEmailRateLimiter CreateLimiter(int? sendRateLimit)
    {
        var settings = new ResiliencySettings
        {
            MailSendRateLimit = sendRateLimit,
            MailSendRateWindow = TimeSpan.FromMinutes(1)
        };

        return new QueuedEmailRateLimiter(settings);
    }
}