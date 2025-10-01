using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class PublicFolderPublisher(IApplicationContext appContext) : IFilePublisher
    {
        private readonly IApplicationContext _appContext = appContext;

        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancelToken)
        {
            var deploymentDir = await context.ExportProfileService.GetDeploymentDirectoryAsync(deployment, true);
            if (deploymentDir == null)
            {
                return;
            }

            var source = _appContext.ContentRoot.AttachEntry(context.ExportDirectory);

            if (context.CreateZipArchive)
            {
                // INFO: Even if it exists, Context.ZipFile.Exists may be "false" here.
                if (context.ZipFile != null)
                {
                    var zipFile = await source.Parent.GetFileAsync(context.ZipFile.Name);

                    using var stream = await zipFile.OpenReadAsync(cancelToken);

                    var newPath = PathUtility.Join(deploymentDir.SubPath, zipFile.Name);
                    var newFile = await deploymentDir.FileSystem.CreateFileAsync(newPath, stream, true, cancelToken);

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
                var target = _appContext.ContentRoot.AttachEntry(deploymentDir);
                await source.FileSystem.CopyDirectoryAsync(source, target);

                context.Log.Info($"Copied export data files to {deploymentDir.SubPath}.");
            }
        }
    }
}
