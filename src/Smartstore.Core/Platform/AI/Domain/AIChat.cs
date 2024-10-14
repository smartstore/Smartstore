#nullable enable

using Newtonsoft.Json;

namespace Smartstore.Core.AI
{
    /// <summary>
    /// Represents an AI conversation consisting of a sequence of messages.
    /// </summary>
    [JsonConverter(typeof(AIChatJsonConverter))]
    public class AIChat(AIChatTopic topic)
    {
        private readonly List<AIChatMessage> _messages = [];

        public AIChatTopic Topic { get; } = topic;

        /// <summary>
        /// The name of the AI model.
        /// <c>null</c> to use the default model.
        /// </summary>
        /// <example>gpt-4o</example>
        public string? ModelName { get; set; }

        public IReadOnlyList<AIChatMessage> Messages 
            => _messages;

        public bool HasMessages()
            => _messages.Count > 0;

        /// <summary>
        /// Adds messages. Empty messages are not added.
        /// </summary>
        public void AddMessages(params AIChatMessage[] messages)
        {
            if (messages != null)
            {
                _messages.AddRange(messages.Where(x => x.Content.HasValue()));
            }
        }

        public override string ToString()
            => string.Join(" ", _messages.Select(x => x.ToString()));
    }
}
