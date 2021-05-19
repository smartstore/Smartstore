using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class FileSystemFilePublisher : IFilePublisher
    {
        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancellationToken)
        {
            var deploymentDir = await context.ExportProfileService.GetDeploymentDirectoryAsync(deployment, true);
            if (deploymentDir == null)
            {
                return;
            }

            // TODO: (mg) (core) complete FileSystemFilePublisher
            // TODO: (mg) (core) Use the new IFileSystemExtensions.AttachEntry() to re-root directories from different file systems to ContentRoot (which is the top-most fs).
            throw new NotImplementedException();

            //if (!FileSystemHelper.CopyDirectory(new DirectoryInfo(context.FolderContent), new DirectoryInfo(targetFolder)))
            //{
            //    context.Result.LastError = context.T("Admin.DataExchange.Export.Deployment.CopyFileFailed");
            //}


            context.Log.Info($"Copied export data files to {deploymentDir.PhysicalPath}.");
        }
    }
}
