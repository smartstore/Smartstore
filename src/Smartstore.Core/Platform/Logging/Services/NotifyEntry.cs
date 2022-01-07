namespace Smartstore.Core.Logging
{
    public enum NotifyType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class NotifyEntry : ComparableObject<NotifyEntry>
    {
        [ObjectSignature]
        public NotifyType Type { get; set; }

        [ObjectSignature]
        public string Message { get; set; }

        public bool Durable { get; set; }
    }

    /// <summary>
    /// For proper JSON serialization
    /// </summary>
    internal class NotifyEntriesHolder
    {
        public NotifyEntry[] Entries { get; set; } = Array.Empty<NotifyEntry>();
    }
}