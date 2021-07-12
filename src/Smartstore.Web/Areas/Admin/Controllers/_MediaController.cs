using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class MediaController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IMediaService _mediaService;
        private readonly IMediaTypeResolver _mediaTypeResolver;
        private readonly MediaSettings _mediaSettings;
        private readonly MediaExceptionFactory _exceptionFactory;

        public MediaController(SmartDbContext db,
            IMediaService mediaService,
            IMediaTypeResolver mediaTypeResolver,
            MediaSettings mediaSettings,
            MediaExceptionFactory exceptionFactory)
        {
            _db = db;
            _mediaService = mediaService;
            _mediaTypeResolver = mediaTypeResolver;
            _mediaSettings = mediaSettings;
            _exceptionFactory = exceptionFactory;
        }

        // TODO: (mh) (core) Testing!
        // TODO: (mh) (core) Fails when trying to add duplicate files. With: Could not determine JSON object type for type Smartstore.Threading.AsyncOut`1[System.String]. at Newtonsoft.Json.Linq.JValue.GetValueType(Nullable`1 current, Object value)
        [HttpPost]
        [Permission(Permissions.Media.Upload)]
        [MaxMediaFileSize]
        public async Task<ActionResult> Upload(
            string path, 
            string[] typeFilter = null, 
            bool isTransient = false,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.ThrowError, 
            string directory = "")
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
                    if (typeFilter != null)
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

                    using var stream = uploadedFile.OpenReadStream();
                    var mediaFile = await _mediaService.SaveFileAsync(filePath, stream, isTransient, duplicateFileHandling);

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

                    var newPath = await _mediaService.CheckUniqueFileNameAsync(filePath);
                    o.uniquePath = newPath;
                    o.createdOn = dupe.CreatedOn.ToString();
                    o.lastUpdated = dupe.LastModified.ToString();

                    result.Add(o);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return Json(result.Count == 1 ? result[0] : result);
        }

        [HttpPost]
        public ActionResult FileConflictResolutionDialog()
        {
            if (!Services.Permissions.Authorize(Permissions.Media.Update))
            {
                throw new AccessDeniedException();
            }

            return PartialView();
        }

        //public ActionResult MoveFsMedia()
        //{
        //    var count = DataMigrator.MoveFsMedia(Services.DbContext);
        //    return Content("Moved and reorganized {0} media files.".FormatInvariant(count));
        //}
    }
}
