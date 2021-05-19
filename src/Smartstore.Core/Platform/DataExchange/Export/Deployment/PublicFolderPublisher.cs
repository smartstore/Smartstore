using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class PublicFolderPublisher : IFilePublisher
    {
        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancellationToken)
        {
            var deploymentDir = await context.ExportProfileService.GetDeploymentDirectoryAsync(deployment, true);
            if (deploymentDir == null)
            {
                return;
            }

            if (context.CreateZipArchive)
            {
                if (context.ZipFile?.Exists ?? false)
                {
                    var newPath = deploymentDir.FileSystem.PathCombine(deploymentDir.SubPath, context.ZipFile.Name);

                    // TODO: (mg) (core) find correct way to copy from 'App_Data/Tenants/Default/ExportProfiles/<subdir> to 'wwwroot/exchange</subdir>'.
                    // MapPathInternal always combines with source root this way.
                    await context.ZipFile.FileSystem.CopyFileAsync(context.ZipFile.SubPath, newPath, true);

                    context.Log.Info($"Copied zipped export data to {newPath}.");
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
