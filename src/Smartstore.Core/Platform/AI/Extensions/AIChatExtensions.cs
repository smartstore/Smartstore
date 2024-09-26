#nullable enable

namespace Smartstore.Core.Platform.AI
{
    public static partial class AIChatExtensions
    {
        /// <summary>
        /// Adds a <see cref="KnownAIMessageRoles.User"/> message.
        /// </summary>
        public static AIChat User(this AIChat chat, string message, string? author = null)
        {
            Guard.NotNull(chat).AddMessages(AIChatMessage.FromUser(message, author));
            return chat;
        }

        /// <summary>
        /// Adds a <see cref="KnownAIMessageRoles.System"/> message.
        /// </summary>
        public static AIChat System(this AIChat chat, string message, string? author = null)
        {
            Guard.NotNull(chat).AddMessages(AIChatMessage.FromSystem(message, author));
            return chat;
        }

        /// <summary>
        /// Adds an <see cref="KnownAIMessageRoles.Assistant"/> message.
        /// </summary>
        public static AIChat Assistant(this AIChat chat, string message, string? author = null)
        {
            Guard.NotNull(chat).AddMessages(AIChatMessage.FromAssistant(message, author));
            return chat;
        }
    }
}
