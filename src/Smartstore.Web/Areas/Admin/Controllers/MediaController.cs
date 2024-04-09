using Newtonsoft.Json.Linq;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Events;

namespace Smartstore.Admin.Controllers
{
    [Area("Admin")]
    [TrackActivity(Order = 100)]
    public class MediaController : SmartController
    {
        private readonly SmartDbContext _db;
        private readonly IMediaService _mediaService;
        private readonly IMediaTypeResolver _mediaTypeResolver;
        private readonly MediaSettings _mediaSettings;
        private readonly MediaExceptionFactory _exceptionFactory;
        private readonly IEventPublisher _eventPublisher;

        public MediaController(SmartDbContext db,
            IMediaService mediaService,
            IMediaTypeResolver mediaTypeResolver,
            MediaSettings mediaSettings,
            MediaExceptionFactory exceptionFactory,
            IEventPublisher eventPublisher)
        {
            _db = db;
            _mediaService = mediaService;
            _mediaTypeResolver = mediaTypeResolver;
            _mediaSettings = mediaSettings;
            _exceptionFactory = exceptionFactory;
            _eventPublisher = eventPublisher;
        }

        [HttpPost]
        [Permission(Permissions.Media.Upload)]
        [ValidateAntiForgeryToken]
        [MaxMediaFileSize]
        public async Task<IActionResult> Upload(
            string path,
            string[] typeFilter = null,
            bool isTransient = false,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.ThrowError,
            string directory = "",
            string entityType = "")
        {
            var numFiles = Request.Form.Files.Count;
            var result = new List<object>(numFiles);

            for (var i = 0; i < numFiles; ++i)
            {
                if (directory.HasValue())
                {
                    path = _mediaService.CombinePaths(path, directory);

                    if (!_mediaService.FolderExists(path))
                    {
                        await _mediaService.CreateFolderAsync(path);
                    }
                }

                var uploadedFile = Request.Form.Files[i];
                var fileName = uploadedFile.FileName;
                var filePath = _mediaService.CombinePaths(path, fileName);

                try
                {
                    // Check if media type or file extension is allowed.
                    var extension = Path.GetExtension(fileName).TrimStart('.').ToLower();
                    if (typeFilter != null && typeFilter.Length > 0)
                    {
                        var mediaTypeExtensions = _mediaTypeResolver.ParseTypeFilter(typeFilter);
                        if (!mediaTypeExtensions.Contains(extension))
                        {
                            throw _exceptionFactory.DeniedMediaType(fileName, extension, typeFilter);
                        }
                    }
                    else
                    {
                        // Check if extension is allowed by media settings.
                        if (!_mediaTypeResolver.GetExtensionMediaTypeMap().Keys.Contains(extension))
                        {
                            throw _exceptionFactory.DeniedMediaType(fileName, extension);
                        }
                    }

                    var mediaFile = await _mediaService.SaveFileAsync(
                        filePath,
                        uploadedFile.OpenReadStream(),
                        isTransient,
                        duplicateFileHandling);

                    await _eventPublisher.PublishAsync(new MediaSavedEvent(mediaFile, entityType));

                    dynamic o = JObject.FromObject(mediaFile);
                    o.success = true;
                    o.createdOn = mediaFile.CreatedOn.ToString();
                    o.lastUpdated = mediaFile.LastModified.ToString();

                    result.Add(o);
                }
                catch (DuplicateMediaFileException dex)
                {
                    var dupe = dex.File;

                    dynamic o = JObject.FromObject(dupe);
                    o.dupe = true;
                    o.errMessage = dex.Message;

                    (await _mediaService.CheckUniqueFileNameAsync(filePath)).Out(out var newPath);
                    o.uniquePath = newPath;
                    o.createdOn = dupe.CreatedOn.ToString();
                    o.lastUpdated = dupe.LastModified.ToString();

                    result.Add(o);
                }
                catch (DeniedMediaTypeException ex)
                {
                    NotifyError(ex.Message);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return Json(result.Count == 1 ? result[0] : result);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Permission(Permissions.Media.Upload)]
        [ValidateAntiForgeryToken]
        public IActionResult FileConflictResolutionDialog()
        {
            // AJAX call
            return PartialView();
        }
    }
}
