#nullable enable

using System.Net;
using System.Net.Http;
using System.Text.Json;
using Smartstore.Json;

namespace Smartstore.Core.AI.Metadata;

/// <summary>
/// Simple HTTP client that fetches AI metadata from the remote AIMetadataManager endpoint.
/// </summary>
public class AIMetadataHttpClient
{
    private readonly HttpClient _httpClient;

    public AIMetadataHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Fetches remote metadata for the given provider from <c>http://localhost/aimetadata/{providerId}</c>.
    /// </summary>
    /// <param name="providerId">The provider ID, e.g. "openai".</param>
    /// <param name="cancelToken">Cancellation token.</param>
    /// <returns>The deserialized <see cref="AIMetadata"/>, or <c>null</c> on failure.</returns>
    public virtual async Task<AIMetadata?> FetchMetadataAsync(string providerId, CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(providerId);

        var url = $"http://localhost:59318/aimetadata/{providerId}";
        var response = await _httpClient.GetAsync(url, cancelToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancelToken);
        if (json.IsNullOrEmpty())
        {
            return null;
        }

        return JsonSerializer.Deserialize<AIMetadata>(json, SmartJsonOptions.CamelCased);
    }
}
