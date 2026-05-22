#nullable enable

namespace Smartstore.Core.Messaging;

public interface IEmailRateLimiter
{
    int GetAllowedSendCount(int requestedCount);
}