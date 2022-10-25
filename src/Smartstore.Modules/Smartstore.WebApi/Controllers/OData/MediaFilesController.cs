using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Autofac.Core;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.OData.ModelBuilder.Core.V1;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.IO;
using Smartstore.Web.Api.Models.OData.Media;

namespace Smartstore.Web.Api.Controllers.OData
{
    // INFO: inheritance from WebApiController<MediaFile> does not work. FileItemInfo.File never gets expanded.
    // INFO: some endpoints are accessible via POST where you would expect GET.
    // That's because a function like GET /MediaFiles/FileExists(Path='content/my-file.jpg') would never work (HTTP status 404).

    /// <summary>
    /// The endpoint for operations on MediaFile entity. Returns type FileItemInfo which wraps and enriches MediaFile.
    /// </summary>
    public class MediaFilesController : WebApiController<FileItemInfo>
    {
        private readonly IMediaService _mediaService;
        private readonly IWebApiService _webApiService;

        public MediaFilesController(IMediaService mediaService, IWebApiService webApiService)
        {
            _mediaService = mediaService;
            _webApiService = webApiService;
        }

        [HttpGet, WebApiQueryable]
        public async Task<IActionResult> Get(ODataQueryOptions<MediaFile> options)
        {
            // See https://github.com/smartstore/Smartstore/issues/481
            var filesQuery = Db.MediaFiles.AsNoTracking().AsQueryable();
            var query = options.ApplyTo(filesQuery) as IQueryable<MediaFile>;

            var files = await query.ToListAsync();
            var result = files.Select(x => Convert(_mediaService.ConvertMediaFile(x)));

            return Ok(result);
        }

        /// <param name="key">The MediaFile identifier.</param>
        [HttpGet, WebApiQueryable]
        public async Task<IActionResult> Get(int key, ODataQueryOptions<MediaFile> options)
        {
            var flags = GetLoadFlags(options);
            var file = await _mediaService.GetFileByIdAsync(key, flags);

            if (file == null)
            {
                return NotFound(key, nameof(MediaFile));
            }

            return Ok(Convert(file));
        }

        [HttpPost, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Post()
        {
            return ErrorResult(null, "POST MediaFiles is not allowed.", Status403Forbidden);
        }

        [HttpPut, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Put()
        {
            return ErrorResult(null, "PUT MediaFiles is not allowed.", Status403Forbidden);
        }

        [HttpPatch, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Patch()
        {
            return ErrorResult(null, "PATCH MediaFiles is not allowed.", Status403Forbidden);
        }

        [HttpDelete, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Delete()
        {
            // Insufficient endpoint. Parameters required but ODataActionParameters not possible here.
            // Query string parameters less good because not part of the EDM.
            return ErrorResult(null, $"DELETE MediaFiles is not allowed. Use action method \"{nameof(DeleteFile)}\" instead.", Status403Forbidden);
        }

        #region Actions and functions

        // TODO: (mg) (core) Swagger examples of unbound ODataActionParameters do not work (always "additionalProp1": "string").
        // https://stackoverflow.com/questions/41141137/how-can-i-tell-swashbuckle-that-the-body-content-is-required
        // Swashbuckle.AspNetCore.Annotations? "SaveFile" requires detailed example including "Path".

        // TODO: (mg) (core) again, to many attribute decorations. This looks like beginner code.
        // Add new smart attribute "ApiActionMethod" that is combining (most of) these things into one?

        /// <summary>
        /// Gets a file by path.
        /// </summary>
        /// <example>POST /MediaFiles/GetFileByPath {"Path":"content/my-file.jpg"}</example>
        [HttpPost, WebApiQueryable]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(FileItemInfo), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> GetFileByPath(ODataActionParameters parameters, ODataQueryOptions<MediaFile> options)
        {
            try
            {
                var path = parameters.GetValueSafe<string>("Path");
                var flags = GetLoadFlags(options);
                var file = await _mediaService.GetFileByPathAsync(path, flags);

                if (file == null)
                {
                    return NotFound($"Cannot find {nameof(MediaFile)} entity with path {path.NaIfEmpty()}.");
                }

                return Ok(Convert(file));
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Gets files by identifiers.
        /// </summary>
        /// <param name="ids">Comma separated list of file identifiers.</param>
        /// <example>GET /MediaFiles/GetFilesByIds(Ids=[1,2,3])</example>
        [HttpGet("MediaFiles/GetFilesByIds(Ids={ids})"), WebApiQueryable]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<FileItemInfo>), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> GetFilesByIds([FromODataUri] int[] ids, ODataQueryOptions<MediaFile> options)
        {
            if (ids.IsNullOrEmpty())
            {
                return Ok(Array.Empty<FileItemInfo>().AsQueryable());
            }

            try
            {
                var flags = GetLoadFlags(options);
                var mediaFiles = await _mediaService.GetFilesByIdsAsync(ids.ToArray(), flags);
                var files = mediaFiles.Select(x => Convert(x)).AsQueryable();

                return Ok(files);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Downloads a file.
        /// </summary>
        /// <param name="id">The MediaFile identifier.</param>
        /// <example>GET /MediaFiles/Download(Id=123)</example>
        [HttpGet("MediaFiles/Download(Id={id})")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> Download(int id)
        {
            var file = await _mediaService.GetFileByIdAsync(id, MediaLoadFlags.WithBlob);
            if (file == null)
            {
                return NotFound(id, nameof(MediaFile));
            }

            try
            {
                var stream = await file.OpenReadAsync();

                return File(stream, file.MimeType, PathUtility.SanitizeFileName(file.Name));
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Searches files that match the filter criteria in query property.
        /// </summary>
        /// <example>POST /MediaFiles/SearchFiles {"Query":{"FolderId":7,"Extensions":["jpg"], ...}}</example>
        [HttpPost, WebApiQueryable]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<FileItemInfo>), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> SearchFiles(ODataActionParameters parameters, ODataQueryOptions<MediaFile> options)
        {
            try
            {
                var state = _webApiService.GetState();
                var query = parameters.GetValueSafe<MediaSearchQuery>("Query") ?? new() { PageSize = state.MaxTop };
                query.PageSize = Math.Min(query.PageSize, state.MaxTop);

                var flags = GetLoadFlags(options);
                var searchResult = await _mediaService.SearchFilesAsync(query, flags);
                var files = searchResult.Select(x => Convert(x)).AsQueryable();

                return Ok(files);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Gets a value indicating whether a file exists.
        /// </summary>
        /// <example>POST /MediaFiles/FileExists {"Path":"content/my-file.jpg"}</example>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(bool), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> FileExists(ODataActionParameters parameters)
        {
            try
            {
                var path = parameters.GetValueSafe<string>("Path");
                var fileExists = await _mediaService.FileExistsAsync(path);

                return Ok(fileExists);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Checks the uniqueness of a file name.
        /// </summary>
        /// <example>POST /MediaFiles/CheckUniqueFileName {"Path":"content/my-file.jpg"}</example>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(CheckUniquenessResult), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> CheckUniqueFileName(ODataActionParameters parameters)
        {
            try
            {
                var path = parameters.GetValueSafe<string>("Path");
                var success = (await _mediaService.CheckUniqueFileNameAsync(path)).Out(out var newPath);

                return Ok(new CheckUniquenessResult
                {
                    Result = success,
                    NewPath = newPath
                });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Ges the number of files that match the filter criteria in query property.
        /// </summary>
        /// <example>POST /MediaFiles/CountFiles {"Query":{"FolderId":7,"Extensions":["jpg"], ...}}</example>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(int), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> CountFiles(ODataActionParameters parameters)
        {
            try
            {
                var query = parameters.GetValueSafe<MediaSearchQuery>("Query");
                var count = await _mediaService.CountFilesAsync(query ?? new MediaSearchQuery());

                return Ok(count);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Gets the number of files that match the filter criteria in Filter property.
        /// </summary>
        /// <example>POST /MediaFiles/CountFilesGrouped {"Filter":{"Term":"my image","Extensions":["jpg"], ...}}</example>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(MediaCountResult), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> CountFilesGrouped(ODataActionParameters parameters)
        {
            try
            {
                var query = parameters.GetValueSafe<MediaFilesFilter>("Filter");
                var fc = await _mediaService.CountFilesGroupedAsync(query ?? new MediaFilesFilter());

                var result = new MediaCountResult
                {
                    Total = fc.Total,
                    Trash = fc.Trash,
                    Unassigned = fc.Unassigned,
                    Transient = fc.Transient,
                    Orphan = fc.Orphan,
                    Folders = fc.Folders
                        .Select(x => new MediaCountResult.FolderCount
                        {
                            FolderId = x.Key,
                            Count = x.Value
                        })
                        .ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Moves a file.
        /// </summary>
        /// <param name="key">The MediaFile identifier.</param>
        /// <example>POST /MediaFiles(123)/MoveFile {"DestinationFileName":"content/updated-file-name.jpg"}</example>
        [HttpPost, WebApiQueryable]
        [Permission(Permissions.Media.Update)]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(FileItemInfo), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> MoveFile(int key, ODataActionParameters parameters)
        {
            try
            {
                var file = await _mediaService.GetFileByIdAsync(key);
                if (file == null)
                {
                    return NotFound(key, nameof(MediaFile));
                }

                var destinationFileName = parameters.GetValueSafe<string>("DestinationFileName");
                var duplicateFileHandling = parameters.GetValueSafe("DuplicateFileHandling", DuplicateFileHandling.ThrowError);
                var movedFile = await _mediaService.MoveFileAsync(file.File, destinationFileName, duplicateFileHandling);

                return Ok(Convert(movedFile));
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Copies a file.
        /// </summary>
        /// <param name="key">The MediaFile identifier.</param>
        /// <example>POST /MediaFiles(123)/CopyFile {"DestinationFileName":"content/new-file.jpg"}</example>
        [HttpPost, WebApiQueryable]
        [Permission(Permissions.Media.Update)]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(MediaFileOperationResult), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> CopyFile(int key, ODataActionParameters parameters)
        {
            try
            {
                var file = await _mediaService.GetFileByIdAsync(key);
                if (file == null)
                {
                    return NotFound(key, nameof(MediaFile));
                }

                var destinationFileName = parameters.GetValueSafe<string>("DestinationFileName");
                var duplicateFileHandling = parameters.GetValueSafe("DuplicateFileHandling", DuplicateFileHandling.ThrowError);
                var copiedFile = await _mediaService.CopyFileAsync(file, destinationFileName, duplicateFileHandling);

                var result = new MediaFileOperationResult
                {
                    DestinationFileId = copiedFile.DestinationFile.Id,
                    IsDuplicate = copiedFile.IsDuplicate,
                    UniquePath = copiedFile.UniquePath,
                    //DestinationFile = Convert(copiedFile.DestinationFile),
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="key">The MediaFile identifier.</param>
        /// <example>POST /MediaFiles(123)/DeleteFile {"Permanent":false}</example>
        [HttpPost]
        [Permission(Permissions.Media.Delete)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(Status204NoContent)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> DeleteFile(int key, ODataActionParameters parameters)
        {
            try
            {
                var file = await _mediaService.GetFileByIdAsync(key);
                if (file == null)
                {
                    return NotFound(key, nameof(MediaFile));
                }

                var permanent = parameters.GetValueSafe<bool>("Permanent");
                var force = parameters.GetValueSafe("Force", false);

                await _mediaService.DeleteFileAsync(file.File, permanent, force);

                return NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Saves a file.
        /// </summary>
        /// <example>POST /MediaFiles/SaveFile &lt;multipart data of a single file&gt;</example>
        [HttpPost, WebApiQueryable]
        [Permission(Permissions.Media.Upload)]
        [Consumes("multipart/form-data")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(FileItemInfo), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status415UnsupportedMediaType)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> SaveFile()
        {
            if (Request.ContentType.IsEmpty() || !Request.ContentType.StartsWithNoCase("multipart/"))
            {
                return StatusCode(Status415UnsupportedMediaType);
            }

            try
            {
                if (Request.Form.Files.Count == 0)
                {
                    ModelState.AddModelError("File", "Missing multipart file data.");
                }
                if (Request.Form.Files.Count > 1)
                {
                    ModelState.AddModelError("File", "Send one file per request, not multiple.");
                }

                var file = Request.Form.Files[0];

                if (file.ContentDisposition.IsEmpty())
                {
                    ModelState.AddModelError("File", "Missing file parameters in content-disposition header.");
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var cd = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
                var path = cd.GetParameterValue<string>("Path");
                var isTransient = cd.GetParameterValue("IsTransient", true);

                var rawDuplicateFileHandling = cd.GetParameterValue<string>("DuplicateFileHandling");
                _ = Enum.TryParse<DuplicateFileHandling>(rawDuplicateFileHandling.EmptyNull(), out var duplicateFileHandling);

                using var stream = file.OpenReadStream();

                var savedFile = await _mediaService.SaveFileAsync(path, stream, isTransient, duplicateFileHandling);

                return Ok(Convert(savedFile));
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion

        private static FileItemInfo Convert(MediaFileInfo file)
        {
            return file != null
                ? MiniMapper.Map<MediaFileInfo, FileItemInfo>(file, CultureInfo.InvariantCulture)
                : null;
        }

        private static MediaLoadFlags GetLoadFlags(ODataQueryOptions<MediaFile> options)
        {
            var flags = MediaLoadFlags.AsNoTracking;
            var expand = options?.SelectExpand?.RawExpand;

            if (expand.HasValue())
            {
                if (expand.ContainsNoCase(nameof(MediaFile.Folder)))
                {
                    flags |= MediaLoadFlags.WithFolder;
                }
                if (expand.ContainsNoCase(nameof(MediaFile.Tracks)))
                {
                    flags |= MediaLoadFlags.WithTracks;
                }
                if (expand.ContainsNoCase(nameof(MediaFile.Tags)))
                {
                    flags |= MediaLoadFlags.WithTags;
                }
            }

            return flags;
        }
    }
}
