using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Localization;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public interface IFilePublisher
    {
        Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context);
    }

    public class ExportDeploymentContext
    {
        public Localizer T { get; set; }
        public ILogger Log { get; set; }

        public string FolderContent { get; set; }

        public string ZipPath { get; set; }
        public bool CreateZipArchive { get; set; }

        public DataDeploymentResult Result { get; set; }

        // TODO: (mg) (core) Rework file system related code in DataExporter.
        //public IEnumerable<string> GetDeploymentFiles()
        //{
        //    if (!CreateZipArchive)
        //    {
        //        return System.IO.Directory.EnumerateFiles(FolderContent, "*", SearchOption.AllDirectories);
        //    }

        //    if (File.Exists(ZipPath))
        //    {
        //        return new string[] { ZipPath };
        //    }

        //    return new string[0];
        //}
    }
}
