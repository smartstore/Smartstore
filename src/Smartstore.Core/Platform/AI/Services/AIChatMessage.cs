#nullable enable

namespace Smartstore.Core.Platform.AI
{
    /// <summary>
    /// Respresents an Open AI chat message role.
    /// See https://platform.openai.com/docs/guides/chat-completions/message-roles
    /// </summary>
    public static partial class KnownAIMessageRoles
    {
        /// <summary>
        /// Used to provide content (requests or comments) for the assistant to respond to.
        /// </summary>
        /// <example>Who won the world series in 2020?</example>
        public const string User = "user";

        /// <summary>
        /// Used to set the behavior of the assistant.
        /// </summary>
        /// <example>You are a helpful assistant.</example>
        public const string System = "system";

        /// <summary>
        /// Used for assistant responses, but can also be written by the client to give examples of desired behavior.
        /// </summary>
        /// <example>The Los Angeles Dodgers won the World Series in 2020.</example>
        public const string Assistant = "assistant";
    }


    /// <summary>
    /// Represents a chat message in an AI conversation history.
    /// </summary>
    public partial class AIChatMessage
    {
        public AIChatMessage(string content, string role, string? name = null)
        {
            Content = content;
            Role = role;
            Name = name;
        }

        /// <summary>
        /// The role of the author of this message. Typically system, user or assistant.
        /// </summary>
        public string Role { get; }

        public string Content { get; }

        /// <summary>
        /// The author's name of this message.
        /// May contain a-z, A-Z, 0-9 and underscores with a maximum length of 64 characters.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Creates a <see cref="KnownAIMessageRoles.User"/> message.
        /// </summary>
        public static AIChatMessage FromUser(string content, string? name = null)
            => new(content, KnownAIMessageRoles.User, name);

        /// <summary>
        /// Creates a <see cref="KnownAIMessageRoles.System"/> message.
        /// </summary>
        public static AIChatMessage FromSystem(string content, string? name = null)
            => new(content, KnownAIMessageRoles.System, name);

        /// <summary>
        /// Creates an <see cref="KnownAIMessageRoles.Assistant"/> message.
        /// </summary>
        public static AIChatMessage FromAssistant(string content, string? name = null)
            => new(content, KnownAIMessageRoles.Assistant, name);
    }
}
