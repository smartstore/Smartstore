#nullable enable

using Smartstore.Core.Platform.AI.Prompting;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Core.Platform.AI
{
    /// <summary>
    /// Represents an AI provider like ChatGPT.
    /// </summary>
    public partial interface IAIProvider : IProvider
    {
        /// <summary>
        /// Gets a value indicating whether the provider is active.
        /// </summary>
        bool IsActive();

        /// <summary>
        /// Gets a value indicating whether the provider supports the given <paramref name="feature"/>.
        /// </summary>
        bool Supports(AIProviderFeatures feature);

        /// <summary>
        /// Gets <see cref="RouteInfo"/> for the given <paramref name="topic"/>.
        /// </summary>
        RouteInfo GetDialogRoute(AIChatTopic topic);

        /// <summary>
        /// Gets the names of the preferred AI models for text generation.
        /// </summary>
        string[] GetPreferredTextModelNames();

        /// <summary>
        /// Gets the names of the preferred AI models for image creation.
        /// </summary>
        string[] GetPreferredImageModelNames();

        /// <summary>
        /// Starts or continues an AI conversation.
        /// Adds the latest answer to <paramref name="chat"/>.
        /// </summary>
        /// <param name="modelName">The name of the AI model, e.g. gpt-4o. <c>null</c> to use the default model.</param>
        /// <returns>Latest answer.</returns>
        /// <exception cref="AIException"></exception>
        // TODO: (mg) (ai) Remove modelName method parameter and move it as property to AIChat. Implement AIChat.UseModel(string) fluent method for convenience.
        Task<string?> ChatAsync(AIChat chat, string? modelName = null, CancellationToken cancelToken = default);

        /// <summary>
        /// Starts or continues an AI conversation.
        /// Adds the latest answer to <paramref name="chat"/>.
        /// </summary>
        /// <param name="modelName">The name of the AI model, e.g. gpt-4o. <c>null</c> to use the default model.</param>
        /// <returns>Latest answer.</returns>
        /// <exception cref="AIException"></exception>
        IAsyncEnumerable<string?> ChatAsStreamAsync(AIChat chat, string? modelName = null, CancellationToken cancelToken = default);

        /// <summary>
        /// Get the URL(s) of AI generated image(s).
        /// </summary>
        /// <param name="model">
        /// The AI prompt model contains all the descriptions and instructions for the AI system to generate an appropriate response.
        /// It also contains additional instructions for image creation.
        /// </param>
        /// <param name="numImages">
        /// The number of images to be generated. Please note that many AI systems such as ChatGPT only generate one image per request.
        /// </param>
        /// <returns>The URL(s) of the generated image(s).</returns>
        /// <exception cref="AIException"></exception>
        Task<string[]?> CreateImagesAsync(IAIImageModel model, int numImages = 1, CancellationToken cancelToken = default);

        /// <summary>
        /// Analyzes an image based on an AI prompt.
        /// </summary>
        /// <param name="url">The image URL.</param>
        /// <param name="prompt">The AI prompt. It contains all the descriptions and instructions for the AI system to generate an appropriate response.</param>
        /// <exception cref="AIException"></exception>
        Task<string> AnalyzeImageAsync(string url, string prompt, CancellationToken cancelToken = default);
    }
}