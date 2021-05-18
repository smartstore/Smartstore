using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Localization;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public interface IFilePublisher
    {
        Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context);
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

        public async Task<IEnumerable<IFile>> GetDeploymentFilesAsync()
        {
            if (CreateZipArchive)
            {
                if (ZipFile?.Exists ?? false)
                {
                    return new[] { ZipFile };
                }
            }
            else
            {
                // Avoid accidents with incalculable consequences due to hundreds of files.
                if (ExportDirectory?.SubPath?.HasValue() ?? false)
                {
                    var files = await ExportDirectory.FileSystem
                    .EnumerateFilesAsync(ExportDirectory.SubPath, "*", true)
                    .ToListAsync();

                    return files;
                }
            }

            return Enumerable.Empty<IFile>();
        }
    }
}
