#nullable enable

using Newtonsoft.Json;

namespace Smartstore.Core.Platform.AI
{
    /// <summary>
    /// Represents an AI conversation consisting of a sequence of messages.
    /// </summary>
    [JsonConverter(typeof(AIChatJsonConverter))]
    public class AIChat
    {
        private readonly List<AIChatMessage> _messages = [];

        public AIChat(params AIChatMessage[] messages)
        {
            AddMessages(messages);
        }

        public IReadOnlyList<AIChatMessage> Messages 
            => _messages;

        public bool HasMessages()
            => _messages.Count > 0;

        /// <summary>
        /// Adds messages. Empty messages are not added.
        /// </summary>
        public void AddMessages(params AIChatMessage[] messages)
        {
            if (messages.IsNullOrEmpty())
            {
                return;
            }

            _messages.AddRange(messages.Where(x => x.Content.HasValue()));
        }

        public override string ToString()
            => string.Join(" ", _messages.Select(x => x.ToString()));
    }
}
