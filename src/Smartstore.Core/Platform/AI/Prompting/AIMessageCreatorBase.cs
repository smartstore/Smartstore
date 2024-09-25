#nullable enable

namespace Smartstore.Core.Platform.AI.Prompting
{
    public abstract class AIMessageCreatorBase(AIMessageBuilder messageBuilder) : IAIMessageCreator
    {
        protected readonly AIMessageBuilder _messageBuilder = messageBuilder;

        /// <summary>
        /// Defines the type for which the prompt generator is responsible.
        /// </summary>
        protected abstract string Type { get; }

        public virtual int Priority => 0;

        /// <inheritdoc/>
        public virtual bool CanHandle(string type)
            => type == Type;

        /// <inheritdoc/>
        public virtual Task<AIChat> BuildTextChatAsync(IAITextModel model)
            => Task.FromResult(new AIChat (AIChatMessage.FromUser(_messageBuilder.Resources.GetResource("Admin.AI.TextCreation.DefaultPrompt", model?.EntityName))));

        /// <inheritdoc/>
        public virtual Task<AIChat> BuildSuggestionChatAsync(IAISuggestionModel model)
            => Task.FromResult(new AIChat(AIChatMessage.FromUser(_messageBuilder.Resources.GetResource("Admin.AI.Suggestions.DefaultPrompt", model?.Input))));

        /// <inheritdoc/>
        public virtual Task<AIChat> BuildImageChatAsync(IAIImageModel model)
        {
            var chat = new AIChat();

            chat.AddMessages(AIChatMessage.FromUser(_messageBuilder.Resources.GetResource("Admin.AI.ImageCreation.DefaultPrompt", model?.EntityName)));

            // Enhance prompt for image creation from model.
            _messageBuilder.BuildImagePrompt(model, chat);

            return Task.FromResult(chat);
        }
    }
}
