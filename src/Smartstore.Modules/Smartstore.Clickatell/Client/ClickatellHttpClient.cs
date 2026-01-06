using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Smartstore.Clickatell.Client.Messages;
using Smartstore.Clickatell.Settings;

namespace Smartstore.Clickatell.Services;

public class ClickatellHttpClient
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public ClickatellHttpClient(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendSmsAsync(string text, ClickatellSettings settings, CancellationToken cancelToken = default)
    {
        if (text.IsEmpty())
        {
            return;
        }

        var data = new Dictionary<string, object>
        {
            ["content"] = text,
            ["to"] = settings.PhoneNumber.SplitSafe(';')
        };
        
        var errorMessage = string.Empty;

        try
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", settings.ApiId);

            using var responseMessage = await _client.PostAsJsonAsync(string.Empty, data, cancelToken);

            if (responseMessage.IsSuccessStatusCode)
            {
                // TODO: (mh) Casing? Check model policy.
                var response = await responseMessage.Content.ReadFromJsonAsync<ClickatellResponse>(cancelToken);

                if (response == null)
                {
                    errorMessage = "Clickatell returned empty response";
                }
                else if (response.Error.HasValue())
                {
                    errorMessage = $"Clickatell API error: {response.Error} (Code: {response.ErrorCode}, Description: {response.ErrorDescription})";
                }
                else if (response.Messages?.Count > 0)
                {
                    var messageErrors = response.Messages
                        .Where(m => m.Error.HasValue())
                        .Select(m => $"{m.To}: {m.Error} (Code: {m.ErrorCode})")
                        .ToList();

                    if (messageErrors.Count != 0)
                    {
                        errorMessage = $"Clickatell message errors: {string.Join("; ", messageErrors)}";
                    }
                }
            }
            else
            {
                var rawResponse = await responseMessage.Content.ReadAsStringAsync(cancelToken);
                errorMessage = $"Clickatell HTTP {(int)responseMessage.StatusCode}: {rawResponse}";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error sending SMS via Clickatell");
        }

        if (errorMessage.HasValue())
        {
            _logger.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }
}