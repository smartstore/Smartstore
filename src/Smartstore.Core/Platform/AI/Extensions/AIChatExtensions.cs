#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Smartstore.Core.AI
{
    public static partial class AIChatExtensions
    {
        /// <summary>
        /// Adds a <see cref="KnownAIMessageRoles.User"/> message.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIChat User(this AIChat chat, string message, string? author = null)
        {
            Guard.NotNull(chat).AddMessages(AIChatMessage.FromUser(message, author));
            return chat;
        }

        /// <summary>
        /// Adds a <see cref="KnownAIMessageRoles.System"/> message.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIChat System(this AIChat chat, string message, string? author = null)
        {
            Guard.NotNull(chat).AddMessages(AIChatMessage.FromSystem(message, author));
            return chat;
        }

        /// <summary>
        /// Adds an <see cref="KnownAIMessageRoles.Assistant"/> message.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIChat Assistant(this AIChat chat, string message, string? author = null)
        {
            Guard.NotNull(chat).AddMessages(AIChatMessage.FromAssistant(message, author));
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
