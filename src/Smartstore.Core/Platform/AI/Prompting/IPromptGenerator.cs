#nullable enable

namespace Smartstore.Core.Platform.AI.Prompting
{
    /// <summary>
    /// Contract to build prompts based on entity type and underlying model.
    /// </summary>
    public interface IPromptGenerator
    {
        /// <summary>
        /// Gets or sets the priority of the prompt generator.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Should return <see langword="true"/> if the implementation handles 
        /// prompt generation for given <paramref name="type"/>.
        /// </summary>
        bool CanHandle(string type);

        /// <summary>
        /// Builds the prompt for given <see cref="IAITextModel"/> model.
        /// </summary>
        /// <returns>The prompt</returns>
        Task<string> GenerateTextPromptAsync(IAITextModel model);

        /// <summary>
        /// Builds the prompt for given <see cref="IAIImageModel"/> model.
        /// </summary>
        /// <returns>The prompt</returns>
        Task<string> GenerateImagePromptAsync(IAIImageModel model);

        /// <summary>
        /// Builds the prompt for given <see cref="IAISuggestionModel"/> model.
        /// </summary>
        /// <returns>The prompt</returns>
        Task<string> GenerateSuggestionPromptAsync(IAISuggestionModel model);
    }
}