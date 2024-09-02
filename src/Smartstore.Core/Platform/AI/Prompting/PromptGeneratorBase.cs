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

        public virtual Task<string> GenerateTextPromptAsync(ITextGenerationPrompt model)
            => Task.FromResult(_promptBuilder.Resources.GetResource("Admin.AI.TextCreation.DefaultPrompt", model?.EntityName));

        public virtual Task<string> GenerateSuggestionPromptAsync(ISuggestionPrompt model)
            => Task.FromResult(_promptBuilder.Resources.GetResource("Admin.AI.Suggestions.DefaultPrompt", model?.Input));

        public virtual Task<string> GenerateImagePromptAsync(IImageGenerationPrompt model)
        {
            var parts = new List<string>
            {
                _promptBuilder.Resources.GetResource("Admin.AI.ImageCreation.DefaultPrompt", model?.EntityName)
            };

            // Enhance prompt for image creation from model.
            _promptBuilder.BuildImagePrompt(model, parts);

            return Task.FromResult(string.Join(" ", parts));
        }
    }
}
