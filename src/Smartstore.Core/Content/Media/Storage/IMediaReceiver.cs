namespace Smartstore.Core.Content.Media.Storage
{
    public interface IMediaReceiver
    {
        /// <summary>
        /// Data received by the source provider to be stored by the target provider
        /// </summary>
        /// <param name="context">Media storage mover context</param>
        /// <param name="mediaFile">Media file item</param>
        /// <param name="stream">Source stream</param>
        Task ReceiveAsync(MediaMoverContext context, MediaFile mediaFile, Stream stream);

        /// <summary>
        /// Called when batch media moving completes
        /// </summary>
        /// <param name="context">Media storage mover context</param>
        /// <param name="succeeded">Whether media moving succeeded</param>
        Task OnCompletedAsync(MediaMoverContext context, bool succeeded, CancellationToken cancelToken);
    }
}
