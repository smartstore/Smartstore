#nullable enable

using System.Net.Http;
using System.Net.Http.Json;
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

        try
        {
            var response = await _httpClient.GetFromJsonAsync<AIMetadata>(url, SmartJsonOptions.CamelCased, cancelToken);
            return response;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
