namespace Smartstore.Core.Logging
{
    /// <summary>
    /// Represents a UI notifier
    /// </summary>
    public interface INotifier
    {
        /// <summary>
        /// Adds a notification to the queue.
        /// </summary>
        /// <param name="type">Type of notification</param>
        /// <param name="message">Message</param>
        void Add(NotifyType type, string message, bool durable = true);

        /// <summary>
        /// Gets all queued notifications for the current request.
        /// </summary>
        ICollection<NotifyEntry> Entries { get; }
    }

    public class Notifier : INotifier
    {
        private readonly HashSet<NotifyEntry> _entries = new();

        public void Add(NotifyType type, string message, bool durable = true)
        {
            _entries.Add(new NotifyEntry { Type = type, Message = message, Durable = durable });
        }

        public ICollection<NotifyEntry> Entries => _entries;
    }
}