#nullable enable

using System.Text;
using Smartstore.Utilities;

namespace Smartstore.Core.AI
{
    /// <summary>
    /// Represents a chat message in an AI conversation history.
    /// </summary>
    public partial class AIChatMessage : IEquatable<AIChatMessage>
    {
        private StringBuilder? _content;

        public AIChatMessage(string? content, string role)
        {
            Role = role;

            if (content != null)
            {
                _content = new StringBuilder(content);
            }
        }

        /// <summary>
        /// Creates a <see cref="KnownAIMessageRoles.User"/> message.
        /// </summary>
        public static AIChatMessage FromUser(string? content)
            => new(content, KnownAIMessageRoles.User);

        /// <summary>
        /// Creates a <see cref="KnownAIMessageRoles.System"/> message.
        /// </summary>
        public static AIChatMessage FromSystem(string? content)
            => new(content, KnownAIMessageRoles.System);

        /// <summary>
        /// Creates an <see cref="KnownAIMessageRoles.Assistant"/> message.
        /// </summary>
        public static AIChatMessage FromAssistant(string? content)
            => new(content, KnownAIMessageRoles.Assistant);

        /// <summary>
        /// The role of the author of this message. Typically system, user or assistant.
        /// </summary>
        public string Role { get; }

        /// <summary>
        /// The message content.
        /// </summary>
        public string? Content
            => _content?.ToString();

        /// <summary>
        /// Appends content to the message.
        /// </summary>
        public void Append(string content)
        {
            if (_content == null)
            {
                _content = new StringBuilder(content);
            }
            else
            {
                _content.Append(content);
            }
        }

        public override string? ToString()
            => _content?.ToString();

        #region Equality

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(typeof(AIChatMessage))
                .Add(Role)
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

            return string.Equals(Role, other.Role) && string.Equals(Content, other.Content);
        }

        #endregion
    }
}
