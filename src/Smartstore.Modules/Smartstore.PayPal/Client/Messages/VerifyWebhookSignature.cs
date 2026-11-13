namespace Smartstore.PayPal.Client.Messages;

public class VerifyWebhookSignature
{
    public string AuthAlgo;
    public string CertUrl;
    public string TransmissionId;
    public string TransmissionSig;
    public string TransmissionTime;
    [JsonConverter(typeof(RawWebhookEventConverter))]
    public string WebhookEvent;
    public string WebhookId;
}

public class RawWebhookEventConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // Preserve the signed event, including whitespace and unknown properties.
        writer.WriteRawValue(value);
    }
}
