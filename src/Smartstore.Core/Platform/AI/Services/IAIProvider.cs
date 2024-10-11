#nullable enable

using Smartstore.Core.AI.Prompting;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.AI
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
        /// Gets the names of the preferred AI models for the given <paramref name="topic"/>.
        /// </summary>
        string[]? GetPreferredModelNames(AIChatTopic topic);

        /// <summary>
        /// Gets the default AI model names.
        /// To be used when <see cref="GetPreferredModelNames(AIChatTopic)"/> returns <c>null</c>.
        /// </summary>
        string[] GetDefaultModelNames();

        /// <summary>
        /// Starts or continues an AI conversation.
        /// Adds the latest answer to <paramref name="chat"/>.
        /// </summary>
        /// <returns>Latest answer.</returns>
        /// <exception cref="AIException"></exception>
        Task<string?> ChatAsync(AIChat chat, CancellationToken cancelToken = default);

        /// <summary>
        /// Starts or continues an AI conversation.
        /// Adds the latest answer to <paramref name="chat"/>.
        /// </summary>
        /// <returns>Latest answer.</returns>
        /// <exception cref="AIException"></exception>
        IAsyncEnumerable<string?> ChatAsStreamAsync(AIChat chat, CancellationToken cancelToken = default);

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