#nullable enable

namespace Smartstore.Core.Platform.AI.Prompting
{
    public abstract class PromptGeneratorBase(PromptBuilder promptBuilder) : IPromptGenerator
    {
        protected readonly PromptBuilder _promptBuilder = promptBuilder;

        /// <summary>
        /// Defines the type for which the prompt generator is responsible.
        /// </summary>
        protected abstract string Type { get; }

        public virtual int Priority => 0;

        public virtual bool CanHandle(string type)
            => type == Type;

        public virtual Task<string> GenerateTextPromptAsync(ITextGenerationPrompt prompt)
            => Task.FromResult(GetDefaultPrompt("Admin.AI.TextCreation.DefaultPrompt", prompt?.EntityName));

        public virtual Task<string> GenerateImagePromptAsync(IImageGenerationPrompt prompt)
            => Task.FromResult(GetDefaultPrompt("Admin.AI.ImageCreation.DefaultPrompt", prompt?.EntityName));

        public virtual Task<string> GenerateSuggestionPromptAsync(ISuggestionPrompt prompt)
            => Task.FromResult(GetDefaultPrompt("Admin.AI.Suggestions.DefaultPrompt", prompt?.Input));

        /// <summary>
        /// Gets a simple default prompt.
        /// </summary>
        /// <param name="key">The string resource key.</param>
        /// <param name="value">The value to get prompt for.</param>
        protected virtual string GetDefaultPrompt(string key, string? value)
        {
            var localizedValue = _promptBuilder.Localization.GetResource(key, returnEmptyIfNotFound: true);
            return localizedValue?.FormatCurrent(value.NaIfEmpty()) ?? string.Empty;
        }
    }
}
