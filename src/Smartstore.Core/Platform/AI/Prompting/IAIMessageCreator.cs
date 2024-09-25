#nullable enable

namespace Smartstore.Core.Platform.AI.Prompting
{
    // TODO: (mh) (ai) Rename --> IAIPromptGenerator (because: no clear distinction between IAIMessageCreator and AIMessageBuilder)
    // TODO: (mh) (ai) Rename methods --> Build*() --> Generate*()
    /// <summary>
    /// Contract to build <see cref="AIChat"/> based on entity type and underlying model.
    /// </summary>
    public interface IAIMessageCreator
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
        /// Builds the <see cref="AIChat"/> for given <see cref="IAITextModel"/> model.
        /// </summary>
        /// <returns>The <see cref="AIChat"/></returns>
        Task<AIChat> BuildTextChatAsync(IAITextModel model);

        /// <summary>
        /// Builds the <see cref="AIChat"/> for given <see cref="IAIImageModel"/> model.
        /// </summary>
        /// <returns>The <see cref="AIChat"/></returns>
        Task<AIChat> BuildImageChatAsync(IAIImageModel model);

        /// <summary>
        /// Builds the <see cref="AIChat"/> for given <see cref="IAISuggestionModel"/> model.
        /// </summary>
        /// <returns>The <see cref="AIChat"/></returns>
        Task<AIChat> BuildSuggestionChatAsync(IAISuggestionModel model);
    }
}