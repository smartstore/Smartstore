using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.AI;
using Smartstore.Core.AI.Metadata;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Platform.AI;

[TestFixture]
public class AIMetadataSerializationTests
{
    [Test]
    public void Can_deserialize_metadata_with_implicit_defaults()
    {
        var json = """
            {
              "version": "2026-07-30",
              "providerId": "test",
              "models": [
                {
                  "id": "fast-model",
                  "type": "text",
                  "level": 0
                },
                {
                  "id": "balanced-model",
                  "type": "image"
                }
              ]
            }
            """;

        var metadata = AIMetadata.FromJson(json)
            ?? throw new AssertionException("Failed to deserialize metadata.");
        var fastModel = metadata.Models.FindModel("fast-model");
        var balancedModel = metadata.Models.FindModel("balanced-model");

        Assert.Multiple(() =>
        {
            metadata.Version.ShouldEqual("2026-07-30");
            metadata.ProviderId.ShouldEqual("test");
            metadata.Models.Count.ShouldEqual(2);

            fastModel.ShouldNotBeNull();
            fastModel.Type.ShouldEqual(AIOutputType.Text);
            fastModel.Level.ShouldEqual(AIModelPerformanceLevel.Fast);
            fastModel.Stream.ShouldBeTrue();

            balancedModel.ShouldNotBeNull();
            balancedModel.Type.ShouldEqual(AIOutputType.Image);
            balancedModel.Level.ShouldEqual(AIModelPerformanceLevel.Balanced);
            balancedModel.Stream.ShouldBeTrue();
        });
    }

    [Test]
    public void Serializes_metadata_to_canonical_json()
    {
        var metadata = new AIMetadata
        {
            Version = "2026-07-30",
            ProviderId = "test",
            ProviderName = string.Empty,
            PostProcessed = true,
            Models =
            [
                new AIModelEntry
                {
                    Id = "fast-model",
                    Name = string.Empty,
                    Type = AIOutputType.Text,
                    Description = string.Empty,
                    Alias = string.Empty,
                    Level = AIModelPerformanceLevel.Fast,
                    Think = new AIModelThinkOptions { Levels = [] },
                    ImageOutputCapabilities = new AIImageOutput(),
                    IsCustom = true
                },
                new AIModelEntry
                {
                    Id = "balanced-model",
                    Type = AIOutputType.Image,
                    Level = AIModelPerformanceLevel.Balanced
                },
                new AIModelEntry
                {
                    Id = "deep-model",
                    Type = AIOutputType.Text,
                    Level = AIModelPerformanceLevel.DeepReasoning,
                    Stream = false
                }
            ]
        };

        using var document = JsonDocument.Parse(metadata.ToJson());
        var root = document.RootElement;
        var models = root.GetProperty("models");
        var fastModel = models[0];
        var balancedModel = models[1];
        var deepModel = models[2];

        Assert.Multiple(() =>
        {
            root.GetProperty("version").GetString().ShouldEqual("2026-07-30");
            root.GetProperty("providerId").GetString().ShouldEqual("test");
            root.TryGetProperty("providerName", out _).ShouldBeFalse();
            root.TryGetProperty("capabilities", out _).ShouldBeFalse();
            root.TryGetProperty("postProcessed", out _).ShouldBeFalse();

            fastModel.TryGetProperty("type", out _).ShouldBeFalse();
            fastModel.GetProperty("level").GetInt32().ShouldEqual(0);
            fastModel.TryGetProperty("name", out _).ShouldBeFalse();
            fastModel.TryGetProperty("description", out _).ShouldBeFalse();
            fastModel.TryGetProperty("alias", out _).ShouldBeFalse();
            fastModel.TryGetProperty("preferred", out _).ShouldBeFalse();
            fastModel.TryGetProperty("vision", out _).ShouldBeFalse();
            fastModel.TryGetProperty("deprecated", out _).ShouldBeFalse();
            fastModel.TryGetProperty("think", out _).ShouldBeFalse();
            fastModel.TryGetProperty("tools", out _).ShouldBeFalse();
            fastModel.TryGetProperty("stream", out _).ShouldBeFalse();
            fastModel.TryGetProperty("output", out _).ShouldBeFalse();
            fastModel.TryGetProperty("isCustom", out _).ShouldBeFalse();

            balancedModel.GetProperty("type").GetString().ShouldEqual("image");
            balancedModel.TryGetProperty("level", out _).ShouldBeFalse();

            deepModel.GetProperty("level").GetInt32().ShouldEqual(2);
            deepModel.GetProperty("stream").GetBoolean().ShouldBeFalse();
        });
    }

    [Test]
    public void Can_roundtrip_complete_metadata()
    {
        var metadata = new AIMetadata
        {
            Version = "2026-07-30 12:30",
            ProviderId = "test",
            ProviderName = "Test Provider",
            Capabilities =
                AIProviderFeatures.TextGeneration |
                AIProviderFeatures.ImageGeneration |
                AIProviderFeatures.ImageAnalysis,
            Models =
            [
                new AIModelEntry
                {
                    Id = "text-model",
                    Name = "Text Model",
                    Type = AIOutputType.Text,
                    Description = "Text model description",
                    Preferred = true,
                    Vision = true,
                    Level = AIModelPerformanceLevel.DeepReasoning,
                    Think = new AIModelThinkOptions
                    {
                        Default = AIReasoningEffort.High,
                        Levels =
                        [
                            AIReasoningEffort.Low,
                            AIReasoningEffort.High,
                            AIReasoningEffort.Max
                        ]
                    },
                    Tools =
                        AIResponseTool.WebSearch |
                        AIResponseTool.CodeAnalysis,
                    Stream = false
                },
                new AIModelEntry
                {
                    Id = "image-model",
                    Name = "Image Model",
                    Type = AIOutputType.Image,
                    Deprecated = true,
                    Alias = "image-model-2",
                    ImageOutputCapabilities = new AIImageOutput
                    {
                        AspectRatios = ["1:1", "16:9"],
                        Resolutions = ["1K", "2K"],
                        Qualities = ["auto", "high"],
                        Formats = ["jpeg", "png"],
                        DefaultAspectRatio = "16:9",
                        DefaultResolution = "2K",
                        DefaultQuality = "high",
                        DefaultFormat = "png",
                        OmitDefault = true
                    }
                }
            ]
        };

        var json = metadata.ToJson();
        var roundtripped = AIMetadata.FromJson(json)
            ?? throw new AssertionException("Failed to deserialize metadata.");
        var reserializedJson = roundtripped.ToJson();
        var textModel = roundtripped.Models.FindModel("text-model");
        var imageModel = roundtripped.Models.FindModel("image-model");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var serializedTextModel = root.GetProperty("models")[0];
        var serializedImageModel = root.GetProperty("models")[1];

        Assert.Multiple(() =>
        {
            JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(reserializedJson)).ShouldBeTrue();
            root.GetProperty("capabilities")
                .EnumerateArray()
                .Select(x => x.GetString())
                .ToArray()
                .ShouldEqual(new string[] { "text", "image", "vision" });
            serializedTextModel.GetProperty("tools")
                .EnumerateArray()
                .Select(x => x.GetString())
                .ToArray()
                .ShouldEqual(new string[] { "WebSearch", "CodeAnalysis" });
            serializedTextModel.GetProperty("think").GetProperty("default").GetString().ShouldEqual("high");
            serializedImageModel.GetProperty("output").GetProperty("defaultFormat").GetString().ShouldEqual("png");

            roundtripped.Version.ShouldEqual(metadata.Version);
            roundtripped.ProviderId.ShouldEqual(metadata.ProviderId);
            roundtripped.ProviderName.ShouldEqual(metadata.ProviderName);
            roundtripped.Capabilities.ShouldEqual(metadata.Capabilities);
            roundtripped.Models.Count.ShouldEqual(2);

            textModel.ShouldNotBeNull();
            textModel.Name.ShouldEqual("Text Model");
            textModel.Type.ShouldEqual(AIOutputType.Text);
            textModel.Description.ShouldEqual("Text model description");
            textModel.Preferred.ShouldBeTrue();
            textModel.Vision.ShouldBeTrue();
            textModel.Level.ShouldEqual(AIModelPerformanceLevel.DeepReasoning);
            textModel.Think.ShouldNotBeNull();
            textModel.Think.Default.ShouldEqual(AIReasoningEffort.High);
            textModel.Think.Levels.ShouldEqual(new AIReasoningEffort[]
            {
                AIReasoningEffort.Low,
                AIReasoningEffort.High,
                AIReasoningEffort.Max
            });
            textModel.Tools.ShouldEqual(AIResponseTool.WebSearch | AIResponseTool.CodeAnalysis);
            textModel.Stream.ShouldBeFalse();

            imageModel.ShouldNotBeNull();
            imageModel.Type.ShouldEqual(AIOutputType.Image);
            imageModel.Deprecated.ShouldBeTrue();
            imageModel.Alias.ShouldEqual("image-model-2");
            imageModel.ImageOutputCapabilities.ShouldNotBeNull();
            imageModel.ImageOutputCapabilities.AspectRatios.ShouldEqual(new string[] { "1:1", "16:9" });
            imageModel.ImageOutputCapabilities.Resolutions.ShouldEqual(new string[] { "1K", "2K" });
            imageModel.ImageOutputCapabilities.Qualities.ShouldEqual(new string[] { "auto", "high" });
            imageModel.ImageOutputCapabilities.Formats.ShouldEqual(new string[] { "jpeg", "png" });
            imageModel.ImageOutputCapabilities.DefaultAspectRatio.ShouldEqual("16:9");
            imageModel.ImageOutputCapabilities.DefaultResolution.ShouldEqual("2K");
            imageModel.ImageOutputCapabilities.DefaultQuality.ShouldEqual("high");
            imageModel.ImageOutputCapabilities.DefaultFormat.ShouldEqual("png");
            imageModel.ImageOutputCapabilities.OmitDefault.ShouldBeTrue();
        });
    }

    [Test]
    public void Can_roundtrip_metadata_through_stream()
    {
        var metadata = new AIMetadata
        {
            Version = "1",
            ProviderId = "test",
            Models =
            [
                new AIModelEntry
                {
                    Id = "model",
                    Type = AIOutputType.Text
                }
            ]
        };

        using var stream = new MemoryStream();
        metadata.ToJson(stream);
        stream.Position = 0;

        var roundtripped = AIMetadata.FromJson(stream)
            ?? throw new AssertionException("Failed to deserialize metadata.");

        Assert.Multiple(() =>
        {
            roundtripped.Version.ShouldEqual("1");
            roundtripped.ProviderId.ShouldEqual("test");
            roundtripped.Models.FindModel("model").ShouldNotBeNull();
        });
    }

    [Test]
    public async Task Can_roundtrip_metadata_through_stream_asynchronously()
    {
        var metadata = new AIMetadata
        {
            Version = "1",
            ProviderId = "test",
            Models =
            [
                new AIModelEntry
                {
                    Id = "model",
                    Type = AIOutputType.Image
                }
            ]
        };

        await using var stream = new MemoryStream();
        await metadata.ToJsonAsync(stream);
        stream.Position = 0;

        var roundtripped = await AIMetadata.FromJsonAsync(stream)
            ?? throw new AssertionException("Failed to deserialize metadata.");

        Assert.Multiple(() =>
        {
            roundtripped.Version.ShouldEqual("1");
            roundtripped.ProviderId.ShouldEqual("test");
            roundtripped.Models.FindModel("model").ShouldNotBeNull();
        });
    }

    [TestCase("""{ "providerId": "test", "models": [] }""")]
    [TestCase("""{ "version": "1", "models": [] }""")]
    public void Required_metadata_properties_cannot_be_omitted(string json)
    {
        Assert.Throws<JsonException>(() => AIMetadata.FromJson(json));
    }

    [TestCase("""{ "version": "1", "providerId": "test", "models": [{ "id": "m", "type": 0 }] }""")]
    [TestCase("""{ "version": "1", "providerId": "test", "models": [{ "id": "m", "type": "audio" }] }""")]
    public void Invalid_output_type_cannot_be_deserialized(string json)
    {
        Assert.Throws<JsonException>(() => AIMetadata.FromJson(json));
    }
}
