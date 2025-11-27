#nullable enable

using Smartstore.Core.AI.Metadata;
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
        /// <returns><c>true</c> if the provider is active, otherwise <c>false</c>.</returns>
        bool IsActive();

        /// <summary>
        /// Gets the metadata associated with the current AI provider, mapped from metadata.json.
        /// </summary>
        AIMetadata Metadata { get; }

        /// <summary>
        /// Gets the local AI models available for the specified topic.
        /// </summary>
        AIModelCollection GetModels(AIChatTopic topic);

        /// <summary>
        /// Retrieves a collection of live AI models that are currently available for use.
        /// </summary>
        Task<AIModelCollection> GetLiveModelsAsync(CancellationToken cancelToken = default);

        /// <summary>
        /// Starts or continues an AI conversation.
        /// Adds the latest answer to the chat if the chat is not of type <see cref="AIChatTopic.Image"/>.
        /// </summary>
        /// <remarks>
        /// For <see cref="AIChatTopic.Image"/>, context information can be passed via the metadata with the key <see cref="KnownAIChatMetadataKeys.ImageChatContext"/>.
        /// </remarks>
        /// <param name="chat">The AI chat.</param>
        /// <returns>
        /// The latest answer or the path of a temporary image file if the chat is of type <see cref="AIChatTopic.Image"/>.
        /// </returns>
        /// <exception cref="AIException">Thrown when an error occurs during the AI conversation.</exception>
        Task<string?> ChatAsync(AIChat chat, CancellationToken cancelToken = default);

        /// <summary>
        /// Starts or continues an AI conversation.
        /// Adds the latest answer to the chat.
        /// </summary>
        /// <param name="chat">The AI chat.</param>
        /// <param name="numAnswers">The number of AI answers to return. 1 by default.</param>
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
        AIImageOptions GetImageOptions(string modelName);

        /// <summary>
        /// Get the URL(s) of AI generated image(s).
        /// </summary>
        /// <param name="model">The AI image model.</param>
        /// <param name="numImages">The number of images to be generated. 1 by default.</param>
        /// <returns>An array of paths of temporary image files.</returns>
        /// <exception cref="AIException">Thrown when an error occurs during image generation.</exception>
        Task<string[]?> CreateImagesAsync(IAIImageModel model, int numImages = 1, CancellationToken cancelToken = default);

        /// <summary>
        /// Analyzes an image based on an AI prompt.
        /// </summary>
        /// <param name="file">Image to analyze.</param>
        /// <param name="chat">The AI chat.</param>
        /// <returns>The analysis result.</returns>
        /// <exception cref="AIException">Thrown when an error occurs during image analysis.</exception>
        Task<string?> AnalyzeImageAsync(MediaFile file, AIChat chat, CancellationToken cancelToken = default);
    }
}