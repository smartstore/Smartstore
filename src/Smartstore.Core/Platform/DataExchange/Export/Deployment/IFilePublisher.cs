using Smartstore.Core.Localization;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public interface IFilePublisher
    {
        /// <summary>
        /// Publishes the result of a file based data export.
        /// </summary>
        /// <param name="deployment">Export deployment.</param>
        /// <param name="context">Deployment context.</param>
        /// <param name="cancelToken">Cancellation token.</param>
        Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancelToken);
    }

    public class ExportDeploymentContext
    {
        public Localizer T { get; init; }
        public ILogger Log { get; init; }
        public IExportProfileService ExportProfileService { get; init; }
        public IDirectory ExportDirectory { get; init; }
        public IFile ZipFile { get; init; }
        public bool CreateZipArchive { get; init; }

        public DataDeploymentResult Result { get; set; }

        /// <summary>
        /// Gets a list of deployment files.
        /// </summary>
        /// <param name="deep">A value indicating whether to get the files from just the top directory or from all sub-directories as well.</param>
        /// <returns>List of deployment files.</returns>
        public async Task<IFile[]> GetDeploymentFilesAsync(bool deep, CancellationToken cancelToken)
        {
            if (!CreateZipArchive)
            {
                return await ExportDirectory
                    .EnumerateFilesAsync("*", deep, cancelToken)
                    .ToArrayAsync(cancelToken);
            }

            if (ZipFile?.Exists ?? false)
            {
                return [ZipFile];
            }

            return [];
        }
    }
}
