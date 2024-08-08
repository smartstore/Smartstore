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
        /// Gets <see cref="RouteInfo"/> for the given <paramref name="modalDialogType"/>.
        /// </summary>
        RouteInfo GetDialogRoute(AIDialogType modalDialogType);

        /// <summary>
        /// Gets the answer for the given <paramref name="prompt"/>.
        /// </summary>
        /// <param name="prompt">The AI prompt. It contains all the descriptions and instructions for the AI system to generate an appropriate response.</param>
        /// <exception cref="AIException"></exception>
        Task<string> ChatAsync(string prompt, CancellationToken cancelToken = default);

        /// <summary>
        /// Gets the answer stream for the given <paramref name="prompt"/>.
        /// </summary>
        /// <param name="prompt">The AI prompt. It contains all the descriptions and instructions for the AI system to generate an appropriate response.</param>
        /// <exception cref="AIException"></exception>
        IAsyncEnumerable<string> ChatAsStreamAsync(string prompt, CancellationToken cancelToken = default);

        /// <summary>
        /// Get the URL(s) of AI generated image(s).
        /// </summary>
        /// <param name="prompt">
        /// The AI prompt. It contains all the descriptions and instructions for the AI system to generate an appropriate response.
        /// It also contains additional instructions for image creation.
        /// </param>
        /// <param name="numImages">
        /// The number of images to be generated. Please note that many AI systems such as ChatGPT only generate one image per request.
        /// </param>
        /// <returns>The URL(s) of the generated image(s).</returns>
        /// <exception cref="AIException"></exception>
        Task<string[]?> CreateImagesAsync(IImageGenerationPrompt prompt, int numImages = 1, CancellationToken cancelToken = default);

        /// <summary>
        /// Analyzes an image based on an AI prompt.
        /// </summary>
        /// <param name="url">The image URL.</param>
        /// <param name="prompt">The AI prompt. It contains all the descriptions and instructions for the AI system to generate an appropriate response.</param>
        /// <exception cref="AIException"></exception>
        Task<string> AnalyzeImageAsync(string url, string prompt, CancellationToken cancelToken = default);
    }
}