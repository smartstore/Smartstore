using CoreFtp;
using CoreFtp.Enum;
using CoreFtp.Infrastructure;

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

            var url = deployment.Url.EmptyNull().Replace('\\', '/');

            if (!url.StartsWith("ftp://", StringComparison.InvariantCultureIgnoreCase) &&
                !url.StartsWith("ftps://", StringComparison.InvariantCultureIgnoreCase))
            {
                url = "ftp://" + url;
            }

            var uri = new Uri(url);
            _rootPath = uri.AbsolutePath.EnsureEndsWith('/');

            var succeededFiles = 0;
            var encryptionType = deployment.UseSsl ? FtpEncryption.Explicit : FtpEncryption.None;
            var port = deployment.UseSsl ? Constants.FtpsPort : Constants.FtpPort;

            // Apply custom port only if explicitly specified.
            if (uri.Authority.Contains(':'))
            {
                port = uri.Port;
            }

            using var client = new FtpClient(new FtpClientConfiguration
            {
                Host = uri.Host,
                Username = deployment.Username,
                Password = deployment.Password,
                Port = port,
                EncryptionType = encryptionType
            });

            await client.LoginAsync();

            if (context.CreateZipArchive)
            {
                if (context.ZipFile?.Exists ?? false)
                {
                    await UploadFile(client, context.ZipFile, _rootPath + context.ZipFile.Name, cancelToken);
                    ++succeededFiles;
                }
            }
            else
            {
                succeededFiles += await UploadDirectory(client, context.ExportDirectory, cancelToken);
            }

            context.Log.Info($"{succeededFiles} file(s) successfully uploaded via FTP.");
        }

        private async Task<int> UploadDirectory(FtpClient client, IDirectory directory, CancellationToken cancelToken)
        {
            if (directory.SubPath.IsEmpty())
            {
                return 0;
            }

            var succeededFiles = 0;
            var files = await directory.EnumerateFilesAsync(cancelToken: cancelToken).ToListAsync(cancelToken);

            foreach (var file in files)
            {
                var targetPath = BuildTargetPath(file);

                await UploadFile(client, file, targetPath, cancelToken);
                ++succeededFiles;
            }

            var subdirs = await directory.EnumerateDirectoriesAsync(cancelToken: cancelToken).ToListAsync(cancelToken);

            foreach (var subdir in subdirs)
            {
                succeededFiles += await UploadDirectory(client, subdir, cancelToken);
            }

            return succeededFiles;
        }

        private static async Task UploadFile(FtpClient client, IFile file, string relativePath, CancellationToken cancelToken)
        {
            await using var targetStream = await client.OpenFileWriteStreamAsync(relativePath);
            await using var sourceStream = await file.OpenReadAsync(cancelToken);

            await sourceStream.CopyToAsync(targetStream, cancelToken);
        }

        private string BuildTargetPath(IFileEntry file)
        {
            return _rootPath + file.SubPath[_context.ExportDirectory.SubPath.Length..].TrimStart(PathUtility.PathSeparators).Replace('\\', '/');
        }
    }
}
