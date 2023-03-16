using System.Text;
using Smartstore.Collections;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents the result of an embedded image offload operation.
    /// </summary>
    public class OffloadImageResult
    {
        /// <summary>
        /// Number of embedded Base64 images that were found in the input HTML.
        /// </summary>
        public int NumAttempted { get; set; }

        /// <summary>
        /// The list of successfully offloaded files.
        /// </summary>
        public IList<MediaFileInfo> OffloadedFiles { get; set; } = new List<MediaFileInfo>();

        /// <summary>
        /// The resulting HTML after all successfully offloaded embedded images 
        /// has been replaced with paths to media storage.
        /// Will be <c>null</c> if no replacement took place.
        /// </summary>
        public string ResultHtml { get; set; }

        /// <summary>
        /// Number of failed offload operations.
        /// </summary>
        public int NumFailed => NumAttempted - OffloadedFiles.Count;

        /// <summary>
        /// Number of succeeded offload operations.
        /// </summary>
        public int NumSucceded => NumAttempted - NumFailed;
    }

    /// <summary>
    /// Represents the result of an embedded image offload batch operation.
    /// </summary>
    public class OffloadImagesBatchResult
    {
        /// <summary>
        /// Total number of all entities containing at least one embedded Base64 image.
        /// </summary>
        public int NumAffectedEntities { get; set; }

        /// <summary>
        /// Number of entities processed during the current batch operation.
        /// </summary>
        public int NumProcessedEntities { get; set; }

        /// <summary>
        /// Number of embedded Base64 images that were found in all HTML sources.
        /// </summary>
        public int NumAttempted { get; set; }

        /// <summary>
        /// Number of failed offload operations.
        /// </summary>
        public int NumFailed { get; set; }

        /// <summary>
        /// Number of succeeded offload operations.
        /// </summary>
        public int NumSucceded => NumAttempted - NumFailed;

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"NumAffectedEntities: {NumAffectedEntities}");
            sb.AppendLine($"NumProcessedEntities: {NumProcessedEntities}");
            sb.AppendLine($"NumAttempted: {NumAttempted}");
            sb.AppendLine($"NumSucceded: {NumSucceded}");
            sb.Append($"NumFailed: {NumFailed}");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Responsible for the extraction of Base64 images embedded in HTML documents.
    /// For each embedded image, a <see cref="MediaFile"/> instance is created and saved
    /// to media storage. The embedded image match is then replaced with the path to
    /// the media file.
    /// </summary>
    public interface IImageOffloder
    {
        /// <summary>
        /// Checks whether the given <paramref name="html"/> input contains at least
        /// one embedded Base64 image.
        /// </summary>
        /// <param name="html">The input HTML to check.</param>
        bool HasEmbeddedImage(string html);

        /// <summary>
        /// Gets the default destination folder for extracted images (file/outsourced).
        /// If the folder does not exist yet, it will be created and returned.
        /// </summary>
        Task<TreeNode<MediaFolderNode>> GetDefaultMediaFolderAsync();

        /// <summary>
        /// Tries to extract embedded Base64 images from a single HTML source.
        /// </summary>
        /// <param name="html">The input HTML from which to extract embedded images from.</param>
        /// <param name="destinationFolder">The destination folder to save media files to.</param>
        /// <param name="entityTag">An entity tag used as file name prefix for the generated file.</param>
        Task<OffloadImageResult> OffloadEmbeddedImagesAsync(string html, MediaFolderNode destinationFolder, string entityTag);

        /// <summary>
        /// Tries to extract embedded Base64 images from all possible HTML document sources.
        /// </summary>
        /// <param name="take">Max number of entities to process.</param>
        Task<OffloadImagesBatchResult> BatchOffloadEmbeddedImagesAsync(int take = 200);
    }
}
