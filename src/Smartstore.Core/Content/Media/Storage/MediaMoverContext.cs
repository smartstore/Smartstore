namespace Smartstore.Core.Content.Media.Storage
{
    public class MediaMoverContext
    {
        internal MediaMoverContext(IMediaSender source, IMediaReceiver target)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(target, nameof(target));

            Source = source;
            Target = target;
        }

        /// <summary>
        /// The system name of the source provider
        /// </summary>
        public IMediaSender Source { get; private set; }

        /// <summary>
        /// The system name of the target provider
        /// </summary>
        public IMediaReceiver Target { get; private set; }

        /// <summary>
        /// Current number of moved media items
        /// </summary>
        public int MovedItems { get; internal set; }

        /// <summary>
        /// Paths of affected media files
        /// </summary>
        public List<string> AffectedFiles { get; private set; } = new();

        /// <summary>
        /// Use this dictionary for any custom data required along the move operation
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();
    }
}