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
            => Task.FromResult(_promptBuilder.GetResource("Admin.AI.TextCreation.DefaultPrompt", prompt?.EntityName));

        public virtual Task<string> GenerateImagePromptAsync(IImageGenerationPrompt prompt)
            => Task.FromResult(_promptBuilder.GetResource("Admin.AI.ImageCreation.DefaultPrompt", prompt?.EntityName));

        public virtual Task<string> GenerateSuggestionPromptAsync(ISuggestionPrompt prompt)
            => Task.FromResult(_promptBuilder.GetResource("Admin.AI.Suggestions.DefaultPrompt", prompt?.Input));
    }
}
