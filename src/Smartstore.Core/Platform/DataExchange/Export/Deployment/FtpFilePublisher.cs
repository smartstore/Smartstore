using FluentFTP;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class FtpFilePublisher : IFilePublisher
    {
        private ExportDeploymentContext _context;
        private string _rootPath;

        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancelToken)
        {
            _context = context;

            var filesSucceeded = 0;
            using var client = CreateClient(deployment);

            try
            {
                await client.Connect(cancelToken);

                if (!context.CreateZipArchive)
                {
                    filesSucceeded += await UploadDirectoryAsync(client, context.ExportDirectory, cancelToken);
                }
                else if (context.ZipFile?.Exists ?? false)
                {
                    if (await UploadFile(client, context.ZipFile, _rootPath + context.ZipFile.Name, true, cancelToken))
                    {
                        ++filesSucceeded;
                    }
                }
            }
            finally
            {
                await client?.Disconnect(cancelToken);
            }

            context.Log.Info("{0} file(s) successfully uploaded via {1}.".FormatInvariant(filesSucceeded, deployment.UseSsl ? "FTPS" : "FTP"));
        }

        private AsyncFtpClient CreateClient(ExportDeployment deployment)
        {
            var url = deployment.Url.EmptyNull().Replace('\\', '/');

            if (!url.StartsWith("ftp://", StringComparison.InvariantCultureIgnoreCase) &&
                !url.StartsWith("ftps://", StringComparison.InvariantCultureIgnoreCase))
            {
                url = "ftp://" + url;
            }

            var uri = new Uri(url);

            _rootPath = uri.AbsolutePath.EnsureEndsWith('/');

            // Apply custom port only if explicitly specified.
            var port = uri.Authority.Contains(':')
                ? uri.Port
                : (deployment.UseSsl ? 990 : 21);

            var client = new AsyncFtpClient(uri.Host, deployment.Username, deployment.Password, port);
            client.Config.EncryptionMode = deployment.UseSsl ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None;

            return client;
        }

        private static async Task<bool> UploadFile(
            AsyncFtpClient client, 
            IFile file, 
            string relativePath, 
            bool createRemoteDir = true,
            CancellationToken cancelToken = default)
        {
            using var sourceStream = await file.OpenReadAsync(cancelToken);
            var status = await client.UploadStream(sourceStream, relativePath, FtpRemoteExists.Overwrite, createRemoteDir, null, cancelToken);
            //$"- ftp {status} {relativePath}".Dump();
            return status != FtpStatus.Failed;
        }

        private async Task<int> UploadDirectoryAsync(AsyncFtpClient client, IDirectory directory, CancellationToken cancelToken)
        {
            if (directory.SubPath.IsEmpty())
            {
                return 0;
            }

            var filesSucceeded = 0;
            var files = await directory.EnumerateFilesAsync(cancelToken: cancelToken).ToListAsync(cancelToken);

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (await UploadFile(client, file, BuildTargetPath(file), i == 0, cancelToken))
                {
                    ++filesSucceeded;
                }
            }

            var subdirs = await directory.EnumerateDirectoriesAsync(cancelToken: cancelToken).ToListAsync(cancelToken);

            foreach (var subdir in subdirs)
            {
                filesSucceeded += await UploadDirectoryAsync(client, subdir, cancelToken);
            }

            return filesSucceeded;
        }

        protected virtual string BuildTargetPath(IFileEntry file)
        {
            return _rootPath + file.SubPath[_context.ExportDirectory.SubPath.Length..].TrimStart(PathUtility.PathSeparators).Replace('\\', '/');
        }
    }
}
