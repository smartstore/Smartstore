using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Imaging;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media
{
    public class MediaHandlerContext
    {
        private IFile _sourceFile;
        private bool _sourceFileResolved;

        public MediaHandlerContext()
        {
            // ...
        }

        public SmartDbContext Db { get; set; }
        public IMediaService MediaService { get; set; }
        public IPermissionService PermissionService { get; set; }
        public Customer CurrentCustomer { get; set; }
        public HttpContext HttpContext { get; set; }

        public int MediaFileId { get; set; }
        public string RawPath { get; set; }

        public MediaPathData PathData { get; set; }
        public ProcessImageQuery ImageQuery { get; set; }

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
                var fi = new FileInfo(CommonHelper.MapPath("~/" + fallbackImagePath, false));
                if (fi.Exists)
                {
                    _sourceFile = new LocalFile(fallbackImagePath, fi, null);
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