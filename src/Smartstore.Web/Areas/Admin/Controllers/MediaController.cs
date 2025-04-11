using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using Smartstore.Admin.Models.Media;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Events;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Admin.Controllers
{
    [Area("Admin")]
    [TrackActivity(Order = 100)]
    public class MediaController : SmartController
    {
        private readonly SmartDbContext _db;
        private readonly IMediaService _mediaService;
        private readonly IMediaTypeResolver _mediaTypeResolver;
        private readonly MediaExceptionFactory _exceptionFactory;
        private readonly IEventPublisher _eventPublisher;
        private readonly Lazy<IMediaMover> _mediaMover;
        private readonly Lazy<ModuleManager> _moduleManager;
        private readonly IProviderManager _providerManager;

        public MediaController(SmartDbContext db,
            IMediaService mediaService,
            IMediaTypeResolver mediaTypeResolver,
            MediaExceptionFactory exceptionFactory,
            IEventPublisher eventPublisher,
            Lazy<IMediaMover> mediaMover,
            Lazy<ModuleManager> moduleManager,
            IProviderManager providerManager)
        {
            _db = db;
            _mediaService = mediaService;
            _mediaTypeResolver = mediaTypeResolver;
            _exceptionFactory = exceptionFactory;
            _eventPublisher = eventPublisher;
            _mediaMover = mediaMover;
            _moduleManager = moduleManager;
            _providerManager = providerManager;
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

                    dynamic o = JObject.FromObject(mediaFile);
                    o.success = true;
                    o.createdOn = mediaFile.CreatedOn.ToString();
                    o.lastUpdated = mediaFile.LastModified.ToString();

                    result.Add(o);

                    await _eventPublisher.PublishAsync(new MediaSavedEvent(mediaFile, entityType));
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

        #region Settings

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> MediaSettings(MediaSettings mediaSettings)
        {
            var model = await MapperFactory.MapAsync<MediaSettings, MediaSettingsModel>(mediaSettings);

            model.CurrentlyAllowedThumbnailSizes = mediaSettings.GetAllowedThumbnailSizes();

            // Media storage provider.
            var currentStorageProvider = Services.Settings.GetSettingByKey<string>("Media.Storage.Provider");
            var provider = _providerManager.GetProvider<IMediaStorageProvider>(currentStorageProvider);

            model.StorageProvider = provider != null ? _moduleManager.Value.GetLocalizedFriendlyName(provider.Metadata) : null;
            ViewBag.AvailableStorageProviders = _providerManager.GetAllProviders<IMediaStorageProvider>()
                .Where(x => !x.Metadata.SystemName.EqualsNoCase(currentStorageProvider))
                .Select(x => new SelectListItem { Text = _moduleManager.Value.GetLocalizedFriendlyName(x.Metadata), Value = x.Metadata.SystemName })
                .ToList();

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, FormValueRequired("save")]
        [SaveSetting]
        public async Task<IActionResult> MediaSettings(MediaSettings settings, MediaSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return await MediaSettings(settings);
            }

            ModelState.Clear();
            await MapperFactory.MapAsync(model, settings);

            NotifySuccess(T("Admin.Configuration.Updated"));
            return RedirectToAction(nameof(MediaSettings));
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost]
        public async Task<IActionResult> ChangeMediaStorage(string targetProvider)
        {
            var currentStorageProvider = Services.Settings.GetSettingByKey<string>("Media.Storage.Provider");
            var source = _providerManager.GetProvider<IMediaStorageProvider>(currentStorageProvider);
            var target = _providerManager.GetProvider<IMediaStorageProvider>(targetProvider);

            var success = await _mediaMover.Value.MoveAsync(source, target);

            if (success)
            {
                NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));
            }

            return RedirectToAction(nameof(MediaSettings));
        }

        #endregion
    }
}
