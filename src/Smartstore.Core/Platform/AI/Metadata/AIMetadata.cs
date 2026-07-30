#nullable enable

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Smartstore.Core.AI.JsonConverters;
using Smartstore.Json;

namespace Smartstore.Core.AI.Metadata;

/// <summary>
/// Root object for metadata.json.
/// </summary>
public class AIMetadata
{
    #region Properties

    /// <summary>
    /// Arbitrary config version or timestamp string.
    /// </summary>
    public required string Version { get; set; } = default!;

    /// <summary>
    /// Internal provider ID (e.g. "openai").
    /// </summary>
    public required string ProviderId { get; set; } = default!;

    /// <summary>
    /// Human-readable provider name.
    /// </summary>
    [DefaultValue("")]
    public string? ProviderName { get; set; }

    /// <summary>
    /// Specifies the capabilities of this provider (e.g. text generation, image generation, translation etc.).
    /// </summary>
    [JsonConverter(typeof(AIProviderFeaturesConverter))]
    public AIProviderFeatures Capabilities { get; set; }

    /// <summary>
    /// List of LLM models available under this provider.
    /// </summary>
    public AIModelCollection Models { get; set; } = [];

    /// <summary>
    /// A flag indicating whether this instance is a post-processed clone.
    /// </summary>
    [IgnoreDataMember]
    public bool PostProcessed { get; set; }

    #endregion

    #region Serialization

    /// <summary>
    /// Serializes this instance to a JSON string.
    /// </summary>
    public string ToJson()
        => JsonSerializer.Serialize(this, SmartJsonOptions.CamelCasedIgnoreDefaults);

    /// <summary>
    /// Serializes this instance to a JSON stream.
    /// </summary>
    /// <param name="utf8Json">The stream to which the JSON will be written.</param>
    public void ToJson(Stream utf8Json)
        => JsonSerializer.Serialize(utf8Json, this, SmartJsonOptions.CamelCasedIgnoreDefaults);

    /// <summary>
    /// Serializes this instance to a JSON stream asynchronously.
    /// </summary>
    /// <param name="utf8Json">The stream to which the JSON will be written.</param>
    public Task ToJsonAsync(Stream utf8Json, CancellationToken cancelToken = default)
        => JsonSerializer.SerializeAsync(utf8Json, this, SmartJsonOptions.CamelCasedIgnoreDefaults, cancelToken);

    /// <summary>
    /// Deserializes an AIMetadata instance from a JSON string.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static AIMetadata? FromJson(string json)
        => JsonSerializer.Deserialize<AIMetadata>(json, SmartJsonOptions.CamelCasedIgnoreDefaults);

    /// <summary>
    /// Deserializes an AIMetadata instance from a JSON stream.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static AIMetadata? FromJson(Stream utf8Json)
        => JsonSerializer.Deserialize<AIMetadata>(utf8Json, SmartJsonOptions.CamelCasedIgnoreDefaults);

    /// <summary>
    /// Deserializes an AIMetadata instance from a JSON stream asynchronously.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static ValueTask<AIMetadata?> FromJsonAsync(Stream utf8Json, CancellationToken cancelToken = default)
        => JsonSerializer.DeserializeAsync<AIMetadata>(utf8Json, SmartJsonOptions.CamelCasedIgnoreDefaults, cancelToken);

    #endregion

    #region Supports...

    public bool Supports(AIProviderFeatures feature)
        => Capabilities.HasFlag(feature);

    public bool SupportsTextGeneration
        => Supports(AIProviderFeatures.TextGeneration);

    public bool SupportsTranslation
        => Supports(AIProviderFeatures.Translation);

    public bool SupportsImageGeneration
        => Supports(AIProviderFeatures.ImageGeneration);

    public bool SupportsImageAnalysis
        => Supports(AIProviderFeatures.ImageAnalysis);

    public bool SuportsThemeVarGeneration
        => Supports(AIProviderFeatures.ThemeVarGeneration);

    public bool SupportsAssistance
        => Supports(AIProviderFeatures.Assistance);

    #endregion

    #region Query models

    /// <summary>
    /// Gets all text models.
    /// </summary>
    /// <param name="preferred">If true, returns only preferred models. If false, returns all other models. If null, returns all undeprecated models.</param>
    public IEnumerable<AIModelEntry> GetTextModels(bool? preferred = null)
        => GetModels(AIOutputType.Text, preferred);

    /// <summary>
    /// Gets all image models.
    /// </summary>
    /// <param name="preferred">If true, returns only preferred models. If false, returns all other models. If null, returns all undeprecated models.</param>
    public IEnumerable<AIModelEntry> GetImageModels(bool? preferred = null)
        => GetModels(AIOutputType.Image, preferred);

    /// <summary>
    /// Gets all models for the given topic.
    /// </summary>
    /// <param name="preferred">If true, returns only preferred models. If false, returns all other models. If null, returns all undeprecated models.</param>
    public IEnumerable<AIModelEntry> GetModels(AIChatTopic topic, bool? preferred = null)
        => GetModels(topic == AIChatTopic.Image ? AIOutputType.Image : AIOutputType.Text, preferred);

    /// <summary>
    /// Gets all models for the given output type.
    /// </summary>
    /// <param name="preferred">If true, returns only preferred models. If false, returns all other models. If null, returns all undeprecated models.</param>
    public IEnumerable<AIModelEntry> GetModels(AIOutputType type, bool? preferred = null)
    {
        return Models.Where(x => x.Type == type && !x.Deprecated && (preferred == null || x.Preferred == preferred.Value)).OrderByDescending(x => x.Preferred);
    }

    /// <summary>
    /// Gets all models that support vision (image analysis).
    /// </summary>
    /// <param name="preferred">If true, returns only preferred models. If false, returns all other models. If null, returns all undeprecated models.</param>
    public IEnumerable<AIModelEntry> GetVisionModels(bool? preferred = null)
    {
        return Models.Where(x => x.Vision && !x.Deprecated && (preferred == null || x.Preferred == preferred.Value)).OrderByDescending(x => x.Preferred);
    }

    /// <summary>
    /// Gets a model by its ID.
    /// </summary>
    /// <param name="mapDeprecated">If true, tries to resolve deprecated models to their alias.</param>
    public AIModelEntry? GetModelById(string modelId, bool mapDeprecated = true)
    {
        if (Models.TryFindModel(modelId, out var modelEntry) && modelEntry.Deprecated && modelEntry.Alias.HasValue() && mapDeprecated)
        {
            // Try to resolve by alias
            modelEntry = Models.FindModel(modelEntry.Alias);
        }

        return modelEntry;
    }

    #endregion

    #region Edit & validate models

    public string ValidateModelId(string modelId, AIOutputType type)
    {
        if (!Models.TryFindModel(modelId, out var modelEntry) || modelEntry.Type != type)
        {
            return GetModels(type, preferred: true).FirstOrDefault()!.Id;
        }

        if (modelEntry != null && modelEntry.Deprecated && modelEntry.Alias.HasValue())
        {
            // Always map deprecated models when validating
            var aliasEntry = Models.FindModel(modelEntry.Alias);
            if (aliasEntry != null && aliasEntry.Type == type && !aliasEntry.Deprecated)
            {
                modelId = aliasEntry.Id;
            }
        }

        return modelId;
    }

    public string ValidateVisionModelId(string modelId)
    {
        if (!Models.TryFindModel(modelId, out var modelEntry) || modelEntry.Type != AIOutputType.Text || !modelEntry.Vision)
        {
            return GetVisionModels(preferred: true).FirstOrDefault()!.Id;
        }

        if (modelEntry != null && modelEntry.Deprecated && modelEntry.Alias.HasValue())
        {
            // Always map deprecated models when validating
            var aliasEntry = Models.FindModel(modelEntry.Alias);
            if (aliasEntry != null && aliasEntry.Type == AIOutputType.Text && modelEntry.Vision && !aliasEntry.Deprecated)
            {
                modelId = aliasEntry.Id;
            }
        }

        return modelId;
    }

    public AIModelCollection MergeTextModels(string[] preferredModelIds)
    {
        return MergeModels(AIOutputType.Text, preferredModelIds);
    }

    public AIModelCollection MergeImageModels(string[] preferredModelIds)
    {
        return MergeModels(AIOutputType.Image, preferredModelIds);
    }

    public AIModelCollection MergeModels(AIOutputType outputType, string[] preferredModelIds)
    {
        if (preferredModelIds.IsNullOrEmpty())
        {
            return [.. GetModels(outputType)];
        }

        var mergedModels = new AIModelCollection();

        foreach (var modelName in preferredModelIds.Distinct())
        {
            var modelEntry = GetModelById(modelName);
            if (modelEntry != null && modelEntry.Type == outputType)
            {
                if (modelEntry.Preferred)
                {
                    mergedModels.Add(modelEntry);
                }
                else
                {
                    // Clone and mark as preferred
                    var cloned = modelEntry.Clone();
                    cloned.Preferred = true;
                    mergedModels.Add(cloned);
                }
            }
            else
            {
                mergedModels.Add(new AIModelEntry
                {
                    Id = modelName,
                    Type = outputType,
                    Preferred = true,
                    Level = AIModelPerformanceLevel.Balanced,
                    IsCustom = true
                });
            }
        }

        var otherModels = GetModels(outputType, preferred: false);

        foreach (var modelEntry in otherModels)
        {
            if (!mergedModels.Contains(modelEntry))
            {
                mergedModels.Add(modelEntry);
            }
        }

        return mergedModels;
    }

    #endregion
}