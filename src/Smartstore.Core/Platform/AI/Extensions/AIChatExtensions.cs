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
        /// <param name="isTopic">A value indicating whether the message is the initial topic message of <paramref name="chat"/>.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIChat User(this AIChat chat, string message, bool isTopic = true)
        {
            // TODO: (mg) Bad API design! Calling this method with isTopic = false is a code smell.
            // - The method should be split into two methods: User and UserTopic (or similar), and/or
            // - AIChat should get a second overload which accepts a message role (e.g. KnownAIMessageRoles.User) that is assigned to TopicMessage.
            var msg = AIChatMessage.FromUser(message);
            Guard.NotNull(chat).AddMessages(msg);

            if (isTopic && chat.TopicMessage == null)
            {
                chat.TopicMessage = msg;
            }

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
