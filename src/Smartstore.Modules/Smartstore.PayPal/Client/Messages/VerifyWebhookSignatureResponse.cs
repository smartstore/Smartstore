#nullable enable

namespace Smartstore.PayPal.Client.Messages;

public class VerifyWebhookSignatureResponse
{
    public string? VerificationStatus { get; set; }
}
