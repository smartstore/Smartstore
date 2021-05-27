using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smartstore.Engine;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class FileSystemFilePublisher : IFilePublisher
    {
        private readonly IApplicationContext _appContext;

        public FileSystemFilePublisher(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancelToken)
        {
            var deploymentDir = await context.ExportProfileService.GetDeploymentDirectoryAsync(deployment, true);
            if (deploymentDir == null)
            {
                return;
            }

            var source = _appContext.ContentRoot.AttachEntry(context.ExportDirectory);
            var webRootDir = await _appContext.WebRoot.GetDirectoryAsync(null);
            var newPath = deploymentDir.FileSystem.PathCombine(webRootDir.Name, deploymentDir.SubPath);

            await source.FileSystem.CopyDirectoryAsync(source.SubPath, newPath);

            context.Log.Info($"Export data files are copied to {newPath}.");
        }
    }
}
