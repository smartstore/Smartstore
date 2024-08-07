using Smartstore.Core.Platform.AI.Prompting;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Core.Platform.AI
{
    // TODO: (mg) add an abstract base class again (the provider may only want to implement a part of it).
    // TODO: (mg) enable nullable.

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
        /// <param name="prompt">The AI prompt. It contains all descriptions and instructions on which the AI system generates a suitable answer.</param>
        /// <exception cref="AIException"></exception>
        Task<string> GetAnswerAsync(string prompt);

        /// <summary>
        /// Gets the answer stream for the given <paramref name="prompt"/>.
        /// </summary>
        /// <param name="prompt">The AI prompt. It contains all descriptions and instructions on which the AI system generates a suitable answer.</param>
        /// <exception cref="AIException"></exception>
        IAsyncEnumerable<string> GetAnswerStreamAsync(string prompt);

        /// <summary>
        /// Get the URL(s) of AI generated image(s).
        /// </summary>
        /// <param name="prompt">
        /// The AI prompt. It contains all descriptions and instructions on which the AI system generates a suitable answer.
        /// It contains additional instructions for image creation.
        /// </param>
        /// <param name="numberOfImages">
        /// The number of images to be generated. Please note that many AI systems such as ChatGPT only generate one image per request.
        /// </param>
        /// <exception cref="AIException"></exception>
        Task<IList<string>> GetImageUrlsAsync(IImageGenerationPrompt prompt, int numberOfImages = 1);

        /// <summary>
        /// Analyzes an image based on an AI prompt.
        /// </summary>
        /// <param name="url">The image URL.</param>
        /// <param name="prompt">The AI prompt. It contains all descriptions and instructions on which the AI system generates a suitable answer.</param>
        /// <exception cref="AIException"></exception>
        Task<string> AnalyzeImageAsync(string url, string prompt);
    }
}