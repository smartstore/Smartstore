#nullable enable

using Smartstore.Core.AI.Prompting;
using Smartstore.Core.Content.Media;
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
        /// <returns>True if the provider is active; otherwise, false.</returns>
        bool IsActive();

        /// <summary>
        /// Gets a value indicating whether the provider supports the given feature.
        /// </summary>
        /// <param name="feature">The AI provider feature.</param>
        /// <returns>True if the provider supports the feature; otherwise, false.</returns>
        bool Supports(AIProviderFeatures feature);

        /// <summary>
        /// Gets the names of the preferred AI models for the given topic.
        /// </summary>
        /// <param name="topic">The AI chat topic.</param>
        /// <returns>An array of preferred AI model names.</returns>
        string[]? GetPreferredModelNames(AIChatTopic topic);

        /// <summary>
        /// Gets the default AI model names.
        /// To be used when GetPreferredModelNames returns null.
        /// </summary>
        /// <returns>An array of default AI model names.</returns>
        string[] GetDefaultModelNames();

        /// <summary>
        /// Starts or continues an AI conversation.
        /// Adds the latest answer to the chat.
        /// </summary>
        /// <param name="chat">The AI chat.</param>
        /// <param name="cancelToken">The cancellation token.</param>
        /// <returns>The latest answer.</returns>
        /// <exception cref="AIException">Thrown when an error occurs during the AI conversation.</exception>
        Task<string?> ChatAsync(AIChat chat, CancellationToken cancelToken = default);

        /// <summary>
        /// Starts or continues an AI conversation.
        /// Adds the latest answer to the chat.
        /// </summary>
        /// <param name="chat">The AI chat.</param>
        /// <param name="numAnswers">The number of AI answers to return. 1 by default.</param>
        /// <param name="cancelToken">The cancellation token.</param>
        /// <returns>The answer and its index. The index is greater than or equal to 0 and less than numAnswers.</returns>
        /// <exception cref="AIException">Thrown when an error occurs during the AI conversation.</exception>
        IAsyncEnumerable<AIChatCompletionResponse> ChatAsStreamAsync(
            AIChat chat,
            int numAnswers = 1,
            CancellationToken cancelToken = default);

        /// <summary>
        /// Gets the image creation or editing options.
        /// </summary>
        /// <param name="modelName">The name of the AI model.</param>
        /// <returns></returns>
        AIImageOptions GetImageOptions(string modelName);

        /// <summary>
        /// Get the URL(s) of AI generated image(s).
        /// </summary>
        /// <param name="model">The AI image model.</param>
        /// <param name="numImages">The number of images to be generated. 1 by default.</param>
        /// <param name="cancelToken">The cancellation token.</param>
        /// <returns>An array of URL(s) of the generated image(s).</returns>
        /// <exception cref="AIException">Thrown when an error occurs during image generation.</exception>
        Task<string[]?> CreateImagesAsync(IAIImageModel model, int numImages = 1, CancellationToken cancelToken = default);

        /// <summary>
        /// Analyzes an image based on an AI prompt.
        /// </summary>
        /// <param name="file">Image to analyze.</param>
        /// <param name="chat">The AI chat.</param>
        /// <param name="cancelToken">The cancellation token.</param>
        /// <returns>The analysis result.</returns>
        /// <exception cref="AIException">Thrown when an error occurs during image analysis.</exception>
        Task<string> AnalyzeImageAsync(MediaFile file, AIChat chat, CancellationToken cancelToken = default);
    }
}