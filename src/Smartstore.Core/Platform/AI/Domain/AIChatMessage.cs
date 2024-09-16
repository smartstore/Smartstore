#nullable enable

namespace Smartstore.Core.Platform.AI
{
    /// <summary>
    /// Represents a chat message in an AI conversation history.
    /// </summary>
    public partial class AIChatMessage
    {
        private string? _content;

        public AIChatMessage(string? content, string role, string? name = null)
        {
            _content = content;
            Role = role;
            Name = name;
        }

        /// <summary>
        /// The role of the author of this message. Typically system, user or assistant.
        /// </summary>
        public string Role { get; }

        /// <summary>
        /// The author's name of this message.
        /// May contain a-z, A-Z, 0-9 and underscores with a maximum length of 64 characters.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets a value indicating whether the message has content.
        /// </summary>
        public bool HasContent()
            => _content.HasValue();

        /// <summary>
        /// Appends content to the message.
        /// </summary>
        public void Append(string? content)
        {
            _content += content;
        }

        /// <summary>
        /// Gets the content of the message.
        /// </summary>
        /// <returns></returns>
        public override string? ToString()
            => _content;

        /// <summary>
        /// Creates a <see cref="KnownAIMessageRoles.User"/> message.
        /// </summary>
        public static AIChatMessage FromUser(string? content, string? name = null)
            => new(content, KnownAIMessageRoles.User, name);

        /// <summary>
        /// Creates a <see cref="KnownAIMessageRoles.System"/> message.
        /// </summary>
        public static AIChatMessage FromSystem(string? content, string? name = null)
            => new(content, KnownAIMessageRoles.System, name);

        /// <summary>
        /// Creates an <see cref="KnownAIMessageRoles.Assistant"/> message.
        /// </summary>
        public static AIChatMessage FromAssistant(string? content, string? name = null)
            => new(content, KnownAIMessageRoles.Assistant, name);
    }
}
