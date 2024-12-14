#nullable enable

namespace Smartstore.Core.AI.Prompting
{
    public abstract class AIPromptGeneratorBase(AIMessageBuilder messageBuilder) : IAIPromptGenerator
    {
        protected readonly AIMessageBuilder _messageBuilder = messageBuilder;

        /// <summary>
        /// Defines the type for which the prompt generator is responsible.
        /// </summary>
        protected abstract string Type { get; }

        public virtual int Priority => 0;

        public virtual bool CanHandle(string type)
            => type == Type;

        public virtual Task<AIChat> GenerateTextChatAsync(IAITextModel model)
            => Task.FromResult(new AIChat(AIChatTopic.Text).User(_messageBuilder.GetDefaultMessage(AIChatTopic.Text, model?.EntityName)));

        public virtual Task<AIChat> GenerateSuggestionChatAsync(IAISuggestionModel model)
            => Task.FromResult(new AIChat(AIChatTopic.Suggestion).User(_messageBuilder.GetDefaultMessage(AIChatTopic.Suggestion, model?.EntityName)));

        public virtual Task<AIChat> GenerateImageChatAsync(IAIImageModel model)
        {
            var chat = new AIChat(AIChatTopic.Image)
                .User(_messageBuilder.GetDefaultMessage(AIChatTopic.Image, model?.EntityName))
                .UseModel(model?.ModelName);

            // Enhance prompt for image creation from model.
            _messageBuilder.BuildImagePrompt(model, chat);

            return Task.FromResult(chat);
        }
    }
}
