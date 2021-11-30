using System;
using System.Threading;
using System.Threading.Tasks;
using CoreFtp;
using CoreFtp.Enum;
using CoreFtp.Infrastructure;
using Dasync.Collections;
using Microsoft.Extensions.Logging;
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

            // TODO: (mg) (core) PassiveMode option not required anymore.
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
            var files = await directory.EnumerateFilesAsync().ToListAsync(cancelToken);

            foreach (var file in files)
            {
                var targetPath = BuildTargetPath(file);

                await UploadFile(client, file, targetPath, cancelToken);
                ++succeededFiles;
            }

            var subdirs = await directory.EnumerateDirectoriesAsync().ToListAsync(cancelToken);

            foreach (var subdir in subdirs)
            {
                succeededFiles += await UploadDirectory(client, subdir, cancelToken);
            }

            return succeededFiles;
        }

        private static async Task UploadFile(FtpClient client, IFile file, string relativePath, CancellationToken cancelToken)
        {
            using var targetStream = await client.OpenFileWriteStreamAsync(relativePath);

            var sourceStream = await file.OpenReadAsync();
            await sourceStream.CopyToAsync(targetStream, cancelToken);
        }

        private string BuildTargetPath(IFileEntry file)
        {
            return _rootPath + file.SubPath[_context.ExportDirectory.SubPath.Length..].TrimStart(PathUtility.PathSeparators).Replace('\\', '/');
        }
    }

    // TODO: (mg) (core) Replace obsolete FtpWebRequest with a 3rdparty FTP library (e.g. "FluentFtp")
    /*
    public class FtpFilePublisher : IFilePublisher
    {
        private ExportDeployment _deployment;
        private ExportDeploymentContext _context;
        private CancellationToken _cancelToken;
        private int _succeededFiles;
        private string _ftpRootUrl;

        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancelToken)
        {
            _deployment = deployment;
            _context = context;
            _cancelToken = cancelToken;
            _succeededFiles = 0;
            _ftpRootUrl = deployment.Url;

            if (!_ftpRootUrl.StartsWith("ftp://", StringComparison.InvariantCultureIgnoreCase))
            {
                _ftpRootUrl = "ftp://" + _ftpRootUrl;
            }

            _ftpRootUrl = _ftpRootUrl.EnsureEndsWith("/");

            if (context.CreateZipArchive)
            {
                if (context.ZipFile?.Exists ?? false)
                {
                    await UploadFile(context.ZipFile, _ftpRootUrl + context.ZipFile.Name, false);
                }
            }
            else
            {
                await FtpCopyDirectory(context.ExportDirectory);
            }

            context.Log.Info($"{_succeededFiles} file(s) successfully uploaded via FTP.");
        }

        private async Task FtpCopyDirectory(IDirectory directory)
        {
            if (directory.SubPath.IsEmpty())
            {
                return;
            }

            var files = await directory.EnumerateFilesAsync().ToListAsync(_cancelToken);
            var lastFile = files.Last();

            foreach (var file in files)
            {
                var url = BuildUrl(file);
                await UploadFile(file, url, file != lastFile);
            }

            var subdirs = await directory.EnumerateDirectoriesAsync().ToListAsync(_cancelToken);
            foreach (var subdir in subdirs)
            {
                var url = BuildUrl(subdir);
                if (!await IsExistingFtpDirectory(url))
                {
                    // You cannot use a FtpWebRequest instance over multiple requests\urls. You can only reuse the underlying FTP connection via KeepAlive.
                    var request = CreateRequest(url, true);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;

                    using var response = (FtpWebResponse)await request.GetResponseAsync();
                    response.Close();
                }

                await FtpCopyDirectory(subdir);
            }
        }

        private async Task<bool> UploadFile(IFile file, string fileUrl, bool keepAlive = true)
        {
            var succeeded = false;
            var request = CreateRequest(fileUrl, keepAlive, file.Length);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            var requestStream = request.GetRequestStream();

            using (var stream = await file.OpenReadAsync())
            {
                await stream.CopyToAsync(requestStream, _cancelToken);
            }

            requestStream.Close();

            using (var response = (FtpWebResponse)await request.GetResponseAsync())
            {
                var statusCode = (int)response.StatusCode;
                succeeded = statusCode >= 200 && statusCode <= 299;

                if (succeeded)
                {
                    ++_succeededFiles;
                }
                else
                {
                    _context.Result.LastError = _context.T("Admin.Common.FtpStatus", statusCode, response.StatusCode.ToString());
                    _context.Log.Error("The FTP transfer failed. FTP status {0} ({1}). File {3}".FormatInvariant(statusCode, response.StatusCode.ToString(), file.PhysicalPath));
                }
            }

            return succeeded;
        }

        private async Task<bool> IsExistingFtpDirectory(string directoryUrl)
        {
            var result = false;

            try
            {
                var request = CreateRequest(directoryUrl.EnsureEndsWith("/"));
                request.Method = WebRequestMethods.Ftp.ListDirectory;

                using var _ = await request.GetResponseAsync();

                result = true;
            }
            catch (WebException)
            {
                result = false;
            }

            return result;
        }

        private FtpWebRequest CreateRequest(string url, bool keepAlive = true, long? contentLength = null)
        {
            var request = (FtpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.KeepAlive = keepAlive;
            request.UseBinary = true;
            request.Proxy = null;
            request.UsePassive = _deployment.PassiveMode;
            request.EnableSsl = _deployment.UseSsl;

            if (_deployment.Username.HasValue())
            {
                request.Credentials = new NetworkCredential(_deployment.Username, _deployment.Password);
            }

            if (contentLength.HasValue)
            {
                request.ContentLength = contentLength.Value;
            }

            return request;
        }

        private string BuildUrl(IFileEntry entry)
        {
            return _ftpRootUrl + entry.SubPath[_context.ExportDirectory.SubPath.Length..].TrimStart('/', '\\').Replace('\\', '/');
        }
    }
    */
}
