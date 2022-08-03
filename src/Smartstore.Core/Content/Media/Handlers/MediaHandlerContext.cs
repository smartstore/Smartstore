using Microsoft.AspNetCore.Http;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Imaging;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public class MediaHandlerContext
    {
        private IFile _sourceFile;
        private bool _sourceFileResolved;

        public IApplicationContext ApplicationContext { get; init; }
        public SmartDbContext Db { get; init; }
        public IMediaService MediaService { get; init; }
        public IPermissionService PermissionService { get; init; }
        public Customer CurrentCustomer { get; init; }
        public HttpContext HttpContext { get; init; }

        public int MediaFileId { get; init; }
        public string RawPath { get; init; }

        public MediaPathData PathData { get; init; }
        public ProcessImageQuery ImageQuery { get; init; }

        public bool Executed { get; set; }
        public Exception Exception { get; set; }
        public IFile ResultFile { get; set; }
        public IImage ResultImage { get; set; }

        public void SetSourceFile(IFile file)
        {
            _sourceFile = file;
            _sourceFileResolved = file != null;
        }

        public async Task<IFile> GetSourceFileAsync()
        {
            if (_sourceFileResolved)
            {
                return _sourceFile;
            }

            if (MediaFileId == 0)
            {
                // This is most likely a request for a default placeholder image
                var fallbackImagePath = Path.Combine(MediaUrlGenerator.FallbackImagesRootPath, RawPath).Replace('\\', '/');
                //var fi = new FileInfo(CommonHelper.MapPath("~/" + fallbackImagePath, false));
                var fallbackFile = ApplicationContext.WebRoot.GetFile(fallbackImagePath);
                if (fallbackFile.Exists)
                {
                    _sourceFile = fallbackFile;
                }
            }
            else
            {
                // Get file from DB
                var mediaFile = await MediaService.GetFileByIdAsync(MediaFileId, MediaLoadFlags.AsNoTracking);

                // File must exist
                if (mediaFile != null)
                {
                    // Serve deleted or hidden files only with sufficient permission
                    if ((mediaFile.Deleted || mediaFile.Hidden) && !PermissionService.Authorize(Permissions.Media.Update, CurrentCustomer))
                        return null;

                    //// File's mime must match requested mime
                    //if (!mediaFile.MimeType.IsCaseInsensitiveEqual(prevMime ?? pathData.MimeType))
                    //	return null;

                    // File path must match requested path and file title
                    // TODO: (mm) (mc) what about MIME and EXT?
                    if (mediaFile.FolderId != PathData.Folder?.Id || !mediaFile.NameWithoutExtension.EqualsNoCase(PathData.FileTitle))
                    {
                        _sourceFileResolved = true;
                        return null;
                    }
                }

                _sourceFile = mediaFile;
            }

            _sourceFileResolved = true;
            return _sourceFile;
        }
    }
}