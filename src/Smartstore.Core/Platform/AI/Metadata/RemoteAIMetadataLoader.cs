#nullable enable

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Smartstore.IO;
using Smartstore.Json;

namespace Smartstore.Core.AI.Metadata;

public class RemoteAIMetadataLoader : IRemoteAIMetadataLoader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApplicationContext _appContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public RemoteAIMetadataLoader(IHttpClientFactory httpClientFactory, IApplicationContext appContext)
    {
        _httpClientFactory = httpClientFactory;
        _appContext = appContext;
        _jsonOptions = SmartJsonOptions.CamelCased;
    }

    private HttpClient HttpClient
        => _httpClientFactory.CreateClient("AIRemoteMetadata");

    public virtual async Task<AIMetadata?> FetchAsync(AIMetadata localMetadata, CancellationToken cancelToken = default)
    {
        Guard.NotNull(localMetadata);

        var cacheFile = GetCacheFile(localMetadata.ProviderId);
        var cached = await ReadCacheAsync(cacheFile, cancelToken);

        // Determine the newest local version to use for a conditional request.
        var newestLocalVersion = GetNewestVersion(localMetadata.Version, cached?.Version);

        var request = new HttpRequestMessage(HttpMethod.Get, $"aimetadata/{localMetadata.ProviderId}");
        if (newestLocalVersion != null)
        {
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{newestLocalVersion}\"", isWeak: false));
        }

        AIMetadata? result;

        try
        {
            var client = HttpClient;
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                result = cached ?? localMetadata;
            }
            else if (response.StatusCode == HttpStatusCode.NoContent)
            {
                result = null;
            }
            else
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancelToken);
                if (json.IsNullOrEmpty())
                {
                    result = cached ?? localMetadata;
                }
                else
                {
                    var metadata = JsonSerializer.Deserialize<AIMetadata>(json, _jsonOptions);
                    if (metadata != null)
                    {
                        await WriteCacheAsync(cacheFile, metadata, cancelToken);
                        metadata.PostProcessed = true;
                    }

                    result = metadata ?? cached ?? localMetadata;
                }
            }
        }
        catch
        {
            // Cloud is unreachable: fall back to cached or local metadata.
            result = cached ?? localMetadata;
        }

        return result;
    }

    private static string? GetNewestVersion(string? a, string? b)
    {
        if (a == null)
        {
            return b;
        }

        if (b == null)
        {
            return a;
        }

        if (DateTimeOffset.TryParse(a, out var da) && DateTimeOffset.TryParse(b, out var db))
        {
            return da >= db ? a : b;
        }

        // If not comparable, prefer the local metadata version.
        return a;
    }

    private async Task<AIMetadata?> ReadCacheAsync(IFile file, CancellationToken cancelToken)
    {
        if (!file.Exists)
        {
            return null;
        }

        try
        {
            await using var stream = await file.OpenReadAsync(cancelToken);
            return await JsonSerializer.DeserializeAsync<AIMetadata>(stream, _jsonOptions, cancelToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task WriteCacheAsync(IFile file, AIMetadata metadata, CancellationToken cancelToken)
    {
        try
        {
            var directory = file.FileSystem.GetDirectory(file.Directory);
            if (!directory.Exists)
            {
                directory.Create();
            }

            await using var stream = file.OpenWrite();
            await JsonSerializer.SerializeAsync(stream, metadata, _jsonOptions, cancelToken);
        }
        catch
        {
            // Best-effort cache write. Failure must not break metadata loading.
        }
    }

    private IFile GetCacheFile(string providerId)
        => _appContext.AppDataRoot.GetFile($"ai/metadata/{providerId}.json");
}
