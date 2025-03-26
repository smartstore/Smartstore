#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Smartstore.Core.AI
{
    public static partial class AIChatExtensions
    {
        /// <summary>
        /// Adds the initial topic <see cref="KnownAIMessageRoles.User"/> message.
        /// It is referenced by <see cref="AIChat.InitialUserMessage"/>.
        /// </summary>
        /// <example>Create a title for a blog post on the topic '{0}'</example>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIChat UserTopic(this AIChat chat, string message)
        {
            var msg = AIChatMessage.FromUser(message);
            Guard.NotNull(chat).AddMessages(msg);

            chat.InitialUserMessage ??= msg;
            return chat;
        }

        /// <summary>
        /// Adds a <see cref="KnownAIMessageRoles.User"/> message.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIChat User(this AIChat chat, string message)
        {
            Guard.NotNull(chat).AddMessages(AIChatMessage.FromUser(message));
            return chat;
        }

        /// <summary>
        /// Adds a <see cref="KnownAIMessageRoles.System"/> message.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIChat System(this AIChat chat, string message)
        {
            Guard.NotNull(chat).AddMessages(AIChatMessage.FromSystem(message));
            return chat;
        }

        /// <summary>
        /// Adds an <see cref="KnownAIMessageRoles.Assistant"/> message.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIChat Assistant(this AIChat chat, string message)
        {
            Guard.NotNull(chat).AddMessages(AIChatMessage.FromAssistant(message));
            return chat;
        }

        /// <summary>
        /// Applies the name of an AI model.
        /// </summary>
        /// <param name="modelName">AI model name.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIChat UseModel(this AIChat chat, string? modelName)
        {
            Guard.NotNull(chat).ModelName = modelName;
            return chat;
        }
    }
}
