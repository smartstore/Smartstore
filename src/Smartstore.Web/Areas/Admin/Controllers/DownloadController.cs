using Smartstore.Core.Content.Media;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Controllers
{
    public class DownloadController : AdminController
    {
        private const string DOWNLOAD_TEMPLATE = "EditorTemplates/Download";

        private readonly SmartDbContext _db;
        private readonly IDownloadService _downloadService;
        private readonly IMediaService _mediaService;
        private readonly MediaSettings _mediaSettings;

        public DownloadController(SmartDbContext db, IDownloadService downloadService, IMediaService mediaService, MediaSettings mediaSettings)
        {
            _db = db;
            _downloadService = downloadService;
            _mediaService = mediaService;
            _mediaSettings = mediaSettings;
        }

        [Permission(Permissions.Media.Download.Read)]
        public async Task<IActionResult> DownloadFile(int downloadId)
        {
            var download = await _db.Downloads
                .Include(x => x.MediaFile)
                .FindByIdAsync(downloadId, false);

            if (download == null)
            {
                return Content(T("Common.Download.NoDataAvailable"));
            }

            if (download.UseDownloadUrl)
            {
                return new RedirectResult(download.DownloadUrl);
            }
            else
            {
                // Use stored data
                var data = await _downloadService.OpenDownloadStreamAsync(download);

                if (data == null || data.Length == 0)
                {
                    return Content(T("Common.Download.NoDataAvailable"));
                }

                var fileName = download.MediaFile.Name;
                var contentType = download.MediaFile.MimeType;

                return new FileStreamResult(data, contentType)
                {
                    FileDownloadName = fileName
                };
            }
        }

        /// <summary>
        /// Used by download editor template to save a complete DownloadUrl via Ajax.
        /// </summary>
        [HttpPost]
        [Permission(Permissions.Media.Download.Create)]
        public async Task<IActionResult> SaveDownloadUrl(string downloadUrl, bool minimalMode = false, string fieldName = null, int entityId = 0, string entityName = "")
        {
            var download = new Download
            {
                EntityId = entityId,
                EntityName = entityName,
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = true,
                DownloadUrl = downloadUrl,
                IsTransient = true,
                UpdatedOnUtc = DateTime.UtcNow
            };

            _db.Downloads.Add(download);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                downloadId = download.Id,
                html = await InvokePartialViewAsync(DOWNLOAD_TEMPLATE, download.Id, new { minimalMode, fieldName })
            });
        }

        [HttpPost]
        [Permission(Permissions.Media.Download.Create)]
        public async Task<IActionResult> CreateDownloadFromMediaFile(int mediaFileId, int entityId = 0, string entityName = "")
        {
            var download = new Download
            {
                EntityId = entityId,
                EntityName = entityName,
                MediaFileId = mediaFileId,
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = false,
                IsTransient = true,
                UpdatedOnUtc = DateTime.UtcNow
            };

            _db.Downloads.Add(download);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                downloadId = download.Id,
            });
        }

        [HttpPost]
        [Permission(Permissions.Media.Download.Create)]
        public async Task<IActionResult> AsyncUpload(string clientCtrlId)
        {
            var postedFile = Request.Form.Files.FirstOrDefault();
            if (postedFile == null)
            {
                throw new ArgumentException(T("Common.NoFileUploaded"));
            }

            var path = _mediaService.CombinePaths(SystemAlbumProvider.Downloads, postedFile.FileName);
            using var stream = postedFile.OpenReadStream();
            Response.RegisterForDispose(stream);

            var file = await _mediaService.SaveFileAsync(path, stream, dupeFileHandling: DuplicateFileHandling.Rename);

            return Json(new
            {
                success = true,
                clientCtrlId,
                id = file.Id,
                name = file.Name,
                type = file.MediaType,
                thumbUrl = await _mediaService.GetUrlAsync(file.Id, _mediaSettings.ProductThumbPictureSize, host: string.Empty)
            });
        }

        [HttpPost]
        [Permission(Permissions.Media.Download.Update)]
        public async Task<IActionResult> AddChangelog(int downloadId, string changelogText)
        {
            var success = false;
            var download = await _db.Downloads.FindByIdAsync(downloadId);

            if (download != null)
            {
                download.Changelog = changelogText;
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Media.Download.Read)]
        public async Task<IActionResult> GetChangelogText(int downloadId)
        {
            var success = false;
            var changeLogText = string.Empty;

            var download = await _db.Downloads.FindByIdAsync(downloadId, false);

            if (download != null)
            {
                changeLogText = download.Changelog;
                success = true;
            }

            return Json(new
            {
                success,
                changelog = changeLogText
            });
        }

        /// <summary>
        /// (ajax) We don't actually delete here. We just return the editor in it's init state.
        /// So the download entity can be set to transient state and deleted later by a scheduled task.
        /// </summary>
        [HttpPost]
        [Permission(Permissions.Media.Download.Delete)]
        public async Task<IActionResult> DeleteDownload(bool minimalMode = false, string fieldName = null)
        {
            return Json(new
            {
                success = true,
                html = await InvokePartialViewAsync(DOWNLOAD_TEMPLATE, null, new { minimalMode, fieldName })
            });
        }
    }
}
