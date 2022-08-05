using Smartstore.IO;

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

            var root = PathUtility.IsAbsolutePhysicalPath(deployment.FileSystemPath.AsSpan())
                ? new LocalFileSystem(Directory.GetDirectoryRoot(deployment.FileSystemPath))
                : _appContext.ContentRoot;

            var source = root.AttachEntry(context.ExportDirectory);
            var target = root.AttachEntry(deploymentDir);

            await source.FileSystem.CopyDirectoryAsync(source, target);

            context.Log.Info($"Export data files are copied to {target.SubPath}.");
        }
    }
}
