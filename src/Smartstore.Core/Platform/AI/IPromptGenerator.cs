using Smartstore.Core.Localization;
using Smartstore.Core.Platform.AI.Chat;

namespace Smartstore.Core.AI
{
    /// <summary>
    /// Creates prompts based on entity type and underlying model.
    /// </summary>
    public interface IPromptGenerator
    {
        /// <summary>
        /// Should return <see langword="true"/> if the implementation class handles 
        /// prompt generation for given <paramref name="type"/>.
        /// </summary>
        bool Match(string type);

        /// <summary>
        /// Gets or sets the priority of the prompt generator.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Builds the prompt for given <see cref="ITextGenerationPrompt"/> model.
        /// </summary>
        /// <returns>The prompt</returns>
        Task<string> GetPromptAsync(ITextGenerationPrompt model)
        {
            // TODO: (mh) Rename --> GenerateTextPromptAsync
            // TODO: (mh) Don't do this. Use ctor dependencies in impl class.
            var value = EngineContext.Current.ResolveService<ILocalizationService>().GetResource(
                "Admin.AI.TextCreation.DefaultPrompt",
                EngineContext.Current.ResolveService<IWorkContext>().WorkingLanguage.Id,
                returnEmptyIfNotFound: true);

            // Simple default implementation
            return Task.FromResult(value.FormatCurrent(model.EntityName));
        }

        /// <summary>
        /// Builds the prompt for given <see cref="IImageGenerationPrompt"/> model.
        /// </summary>
        /// <returns>The prompt</returns>
        Task<string> GetPromptAsync(IImageGenerationPrompt model)
        {
            // TODO: (mh) Rename --> GenerateImagePromptAsync
            // TODO: (mh) Don't do this. Use ctor dependencies in impl class.
            var value = EngineContext.Current.ResolveService<ILocalizationService>().GetResource(
                "Admin.AI.ImageCreation.DefaultPrompt",
                EngineContext.Current.ResolveService<IWorkContext>().WorkingLanguage.Id,
                returnEmptyIfNotFound: true);

            // Simple default implementation
            return Task.FromResult(value.FormatCurrent(model.EntityName));
        }

        /// <summary>
        /// Builds the prompt for given <see cref="ISuggestionPrompt"/> model.
        /// </summary>
        /// <returns>The prompt</returns>
        Task<string> GetPromptAsync(ISuggestionPrompt model)
        {
            // TODO: (mh) Rename --> GenerateSuggestionPromptAsync
            // TODO: (mh) Don't do this. Use ctor dependencies in impl class.
            var value = EngineContext.Current.ResolveService<ILocalizationService>().GetResource(
                "Admin.AI.Suggestions.DefaultPrompt",
                EngineContext.Current.ResolveService<IWorkContext>().WorkingLanguage.Id,
                returnEmptyIfNotFound: true);

            // Simple default implementation
            return Task.FromResult(value.FormatCurrent(model.Input));
        }
    }
}