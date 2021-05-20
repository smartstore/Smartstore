using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smartstore.Engine;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class PublicFolderPublisher : IFilePublisher
    {
        private readonly IApplicationContext _appContext;

        public PublicFolderPublisher(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancellationToken)
        {
            var deploymentDir = await context.ExportProfileService.GetDeploymentDirectoryAsync(deployment, true);
            if (deploymentDir == null)
            {
                return;
            }

            var root = _appContext.ContentRoot.AttachEntry(context.ExportDirectory);

            if (context.CreateZipArchive)
            {
                if (context?.ZipFile?.Exists ?? false)
                {
                    var zipPath = root.FileSystem.PathCombine(root.Parent.SubPath, context.ZipFile.Name);
                    var zipFile = await root.FileSystem.GetFileAsync(zipPath);

                    using var stream = await zipFile.OpenReadAsync();

                    var newPath = deploymentDir.FileSystem.PathCombine(deploymentDir.SubPath, zipFile.Name);
                    var newFile = await deploymentDir.FileSystem.CreateFileAsync(newPath, stream, true);

                    if (newFile?.Exists ?? false)
                    {
                        context.Log.Info($"Copied zipped export data to {newPath}.");
                    }
                    else
                    {
                        context.Log.Warn($"Failed to copy zipped export data to {newPath}.");
                    }
                }
            }
            else
            {
                
            }

            // TODO: (mg) (core) complete PublicFolderPublisher
            throw new NotImplementedException();
        }
    }
}
