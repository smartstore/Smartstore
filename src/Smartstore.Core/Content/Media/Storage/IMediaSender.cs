namespace Smartstore.Core.Content.Media.Storage
{
    public interface IMediaSender
    {
        /// <summary>
        /// Moves a media item to the target provider
        /// </summary>
        /// <param name="target">Target provider</param>
        /// <param name="context">Media storage mover context</param>
        /// <param name="mediaFile">Media file item</param>
        Task MoveToAsync(IMediaReceiver target, MediaMoverContext context, MediaFile mediaFile);

        /// <summary>
        /// Called when batch media moving completes
        /// </summary>
        /// <param name="context">Media storage mover context</param>
        /// <param name="succeeded">Whether media moving succeeded</param>
        Task OnCompletedAsync(MediaMoverContext context, bool succeeded, CancellationToken cancelToken);
    }
}
