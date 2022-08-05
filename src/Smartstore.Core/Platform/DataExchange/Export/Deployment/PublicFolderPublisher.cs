using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class PublicFolderPublisher : IFilePublisher
    {
        private readonly IApplicationContext _appContext;

        public PublicFolderPublisher(IApplicationContext appContext)
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

            if (context.CreateZipArchive)
            {
                if (context?.ZipFile?.Exists ?? false)
                {
                    var zipFile = await source.Parent.GetFileAsync(context.ZipFile.Name);

                    using var stream = await zipFile.OpenReadAsync(cancelToken);

                    var newPath = PathUtility.Join(deploymentDir.SubPath, zipFile.Name);
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
                // Ugly, but the only way I got it to work with CopyDirectoryAsync.
                var webRootDir = await _appContext.WebRoot.GetDirectoryAsync(null);
                var newPath = PathUtility.Join(webRootDir.Name, deploymentDir.SubPath);

                await source.FileSystem.CopyDirectoryAsync(source.SubPath, newPath);

                context.Log.Info($"Export data files are copied to {newPath}.");
            }
        }
    }
}
