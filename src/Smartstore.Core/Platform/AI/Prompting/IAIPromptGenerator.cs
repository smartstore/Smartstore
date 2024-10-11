#nullable enable

namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Contract to generate <see cref="AIChat"/> based on entity type and underlying model.
    /// </summary>
    public interface IAIPromptGenerator
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
        /// Generates the <see cref="AIChat"/> for given <see cref="IAITextModel"/> model.
        /// </summary>
        /// <returns>The <see cref="AIChat"/></returns>
        Task<AIChat> GenerateTextChatAsync(IAITextModel model);

        /// <summary>
        /// Generates the <see cref="AIChat"/> for given <see cref="IAIImageModel"/> model.
        /// </summary>
        /// <returns>The <see cref="AIChat"/></returns>
        Task<AIChat> GenerateImageChatAsync(IAIImageModel model);

        /// <summary>
        /// Generates the <see cref="AIChat"/> for given <see cref="IAISuggestionModel"/> model.
        /// </summary>
        /// <returns>The <see cref="AIChat"/></returns>
        Task<AIChat> GenerateSuggestionChatAsync(IAISuggestionModel model);
    }
}