#nullable enable

using Smartstore.Utilities;

namespace Smartstore.Core.Platform.AI
{
    /// <summary>
    /// Represents a chat message in an AI conversation history.
    /// </summary>
    public partial class AIChatMessage : IEquatable<AIChatMessage>
    {
        private string? _content;

        public AIChatMessage(string? content, string role, string? name = null)
        {
            _content = content;
            Role = role;
            Name = name;
        }

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
        /// The message content.
        /// </summary>
        public string? Content
            => _content;

        /// <summary>
        /// Appends content to the message.
        /// </summary>
        public void Append(string? content)
        {
            _content += content;
        }

        public override string? ToString()
            => _content;

        #region Equality

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(typeof(AIChatMessage))
                .Add(Role)
                .Add(Name)
                .Add(Content)
                .CombinedHash;
        }

        public override bool Equals(object? obj)
        {
            return ((IEquatable<AIChatMessage>)this).Equals(obj as AIChatMessage);
        }

        bool IEquatable<AIChatMessage>.Equals(AIChatMessage? other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                string.Equals(Role, other.Role) &&
                string.Equals(Name, other.Name) &&
                string.Equals(Content, other.Content);
        }

        #endregion
    }
}
