#nullable enable

namespace Smartstore.PayPal.Client.Messages;

public class VerifyWebhookSignatureResponse
{
    [JsonPropertyName("verification_status")]
    public string? VerificationStatus { get; set; }
}
