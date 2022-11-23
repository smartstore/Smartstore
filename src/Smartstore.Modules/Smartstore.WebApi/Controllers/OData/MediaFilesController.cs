using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.ModelBuilder.Core.V1;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.IO;
using Smartstore.Web.Api.Models.Media;

namespace Smartstore.Web.Api.Controllers.OData
{
    // INFO: inheritance from WebApiController<MediaFile> does not work. FileItemInfo.File never gets expanded.
    // INFO: some endpoints are accessible via POST where you would expect GET.
    // That's because a function like GET /MediaFiles/FileExists(Path='content/my-file.jpg') would never work (HTTP status 404).

    // TODO: (mg) (core) $filter, $expand does not validate due to wrong EDM model type. Expects FileItemInfo but we need to query MediaFile.

    /// <summary>
    /// The endpoint for operations on MediaFile entity. Returns type FileItemInfo which wraps and enriches MediaFile.
    /// </summary>
    [ProducesResponseType(Status422UnprocessableEntity)]
    public class MediaFilesController : WebApiController<FileItemInfo>
    {
        private readonly IMediaService _mediaService;
        private readonly IWebApiService _webApiService;

        public MediaFilesController(IMediaService mediaService, IWebApiService webApiService)
        {
            _mediaService = mediaService;
            _webApiService = webApiService;
        }

        [HttpGet, ApiQueryable(AllowedQueryOptions = AllowedQueryOptions.Top | AllowedQueryOptions.Skip | AllowedQueryOptions.Count)]
        public async Task<IActionResult> Get(ODataQueryOptions<MediaFile> options)
        {
            // See https://github.com/smartstore/Smartstore/issues/481
            var filesQuery = Db.MediaFiles.AsNoTracking().AsQueryable();
            var query = options.ApplyTo(filesQuery) as IQueryable<MediaFile>;

            var files = await query.ToListAsync();
            var result = files.Select(x => Convert(_mediaService.ConvertMediaFile(x)));

            return Ok(result);
        }

        [HttpGet, ApiQueryable]
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
            return Forbidden();
        }

        [HttpPut, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Put()
        {
            return Forbidden();
        }

        [HttpPatch, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Patch()
        {
            return Forbidden();
        }

        [HttpDelete, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Delete()
        {
            // Insufficient endpoint. Parameters required but ODataActionParameters not possible here.
            // Query string parameters less good because not part of the EDM.
            return Forbidden($"Use endpoint \"{nameof(DeleteFile)}\" instead.");
        }

        #region Actions and functions

        /// <summary>
        /// Gets a file by path.
        /// </summary>
        /// <param name="path" example="content/my-file.jpg">The path of the file.</param>
        [HttpPost("MediaFiles/GetFileByPath"), ApiQueryable]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(FileItemInfo), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> GetFileByPath([FromODataBody, Required] string path, ODataQueryOptions<MediaFile> options)
        {
            try
            {
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
        /// <param name="ids" example="[1,2,3]">Comma separated list of MediaFile identifiers.</param>
        [HttpGet("MediaFiles/GetFilesByIds(ids={ids})"), ApiQueryable]
        [Produces(Json)]
        [ProducesResponseType(typeof(IEnumerable<FileItemInfo>), Status200OK)]
        public async Task<IActionResult> GetFilesByIds([FromODataUri, Required] int[] ids, ODataQueryOptions<MediaFile> options)
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
        [HttpGet("MediaFiles/DownloadFile(id={id})")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> DownloadFile(int id)
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

        // TODO: (mg) (core) produces ODataException: The property 'folderId' does not exist on type 'Smartstore.Core.Content.Media.MediaSearchQuery'.
        // Changing ODataMessageReaderSettings without any effect.

        /// <summary>
        /// Searches for files using filter criteria.
        /// </summary>
        /// <param name="query">The query that defines the search criteria.</param>
        [HttpPost("MediaFiles/SearchFiles"), ApiQueryable]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(IEnumerable<FileItemInfo>), Status200OK)]
        public async Task<IActionResult> SearchFiles([FromODataBody] MediaSearchQuery query, ODataQueryOptions<MediaFile> options)
        {
            try
            {
                var state = _webApiService.GetState();
                query ??= new() { PageSize = state.MaxTop };
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
        /// Ges the number of files that match the filter criteria in query property.
        /// </summary>
        /// <param name="query">The query that defines the filter criteria.</param>
        [HttpPost("MediaFiles/CountFiles")]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(int), Status200OK)]
        public async Task<IActionResult> CountFiles([FromODataBody] MediaSearchQuery query)
        {
            try
            {
                var count = await _mediaService.CountFilesAsync(query ?? new MediaSearchQuery());

                return Ok(count);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Gets the number of files that match filter criteria.
        /// </summary>
        /// <param name="filter">Filter criteria.</param>
        [HttpPost("MediaFiles/CountFilesGrouped")]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(MediaCountResult), Status200OK)]
        public async Task<IActionResult> CountFilesGrouped([FromODataBody] MediaFilesFilter filter)
        {
            try
            {
                var fc = await _mediaService.CountFilesGroupedAsync(filter ?? new MediaFilesFilter());

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
        /// Gets a value indicating whether a file exists.
        /// </summary>
        /// <param name="path" example="content/my-file.jpg">The path of the file.</param>
        [HttpPost("MediaFiles/FileExists")]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(bool), Status200OK)]
        public async Task<IActionResult> FileExists([FromODataBody, Required] string path)
        {
            try
            {
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
        /// <param name="path" example="content/my-file.jpg">The path of the file.</param>
        [HttpPost("MediaFiles/CheckUniqueFileName")]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(CheckUniquenessResult), Status200OK)]
        public async Task<IActionResult> CheckUniqueFileName([FromODataBody, Required] string path)
        {
            try
            {
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
        /// Moves a file.
        /// </summary>
        /// <param name="destinationFileName" example="content/updated-file-name.jpg">The destination file name.</param>
        /// <param name="duplicateFileHandling" example="0">A value indicating how to proceed if the destination file already exists.</param>
        [HttpPost("MediaFiles({key})/MoveFile"), ApiQueryable]
        [Permission(Permissions.Media.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(FileItemInfo), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> MoveFile(int key,
            [FromODataBody, Required] string destinationFileName,
            [FromODataBody] DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.ThrowError)
        {
            try
            {
                var file = await _mediaService.GetFileByIdAsync(key);
                if (file == null)
                {
                    return NotFound(key, nameof(MediaFile));
                }

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
        /// <param name="destinationFileName" example="content/new-file.jpg">The destination file name.</param>
        /// <param name="duplicateFileHandling" example="0">A value indicating how to proceed if the destination file already exists.</param>
        [HttpPost("MediaFiles({key})/CopyFile"), ApiQueryable]
        [Permission(Permissions.Media.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(MediaFileOperationResult), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> CopyFile(int key,
            [FromODataBody, Required] string destinationFileName,
            [FromODataBody] DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.ThrowError)
        {
            try
            {
                var file = await _mediaService.GetFileByIdAsync(key);
                if (file == null)
                {
                    return NotFound(key, nameof(MediaFile));
                }

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
        /// <param name="permanent" example="false">A value indicating whether to permanently delete the file.</param>
        /// <param name="force" example="false">A value indicating whether to delete the file if it is referenced by another entity.</param>
        [HttpPost("MediaFiles({key})/DeleteFile")]
        [Permission(Permissions.Media.Delete)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(Status204NoContent)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> DeleteFile(int key,
            [FromODataBody, Required] bool permanent,
            [FromODataBody] bool force = false)
        {
            try
            {
                var file = await _mediaService.GetFileByIdAsync(key);
                if (file == null)
                {
                    return NotFound(key, nameof(MediaFile));
                }

                await _mediaService.DeleteFileAsync(file.File, permanent, force);

                return NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        // INFO: bug in Swashbuckle 6.4.0: code comments of parameters decorated with "FromFormAttribute" do not show up in Swagger.
        // https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2519

        /// <summary>
        /// Saves a file.
        /// </summary>
        /// <param name="file">The file to be saved.</param>
        /// <param name="path" example="file/my-file.jpg">The path of the file.</param>
        /// <param name="isTransient" example="true">A value indicating whether the file is transient/preliminary.</param>
        /// <param name="duplicateFileHandling">A value of indicating how to proceed if the uploaded file already exists.</param>
        [HttpPost("MediaFiles/SaveFile"), ApiQueryable]
        [Permission(Permissions.Media.Upload)]
        [Consumes("multipart/form-data"), Produces(Json)]
        [ProducesResponseType(typeof(FileItemInfo), Status200OK)]
        [ProducesResponseType(Status415UnsupportedMediaType)]
        public async Task<IActionResult> SaveFile(
            [Required] IFormFile file,
            [FromForm] string path,
            [FromForm] bool isTransient = true,
            [FromForm] DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.ThrowError)
        {
            if (Request.ContentType.IsEmpty() || !Request.ContentType.StartsWithNoCase("multipart/"))
            {
                return StatusCode(Status415UnsupportedMediaType);
            }

            try
            {
                if (Request.Form.Files.Count == 0)
                {
                    return BadRequest("Missing multipart file data.");
                }
                if (Request.Form.Files.Count > 1)
                {
                    return BadRequest("Send one file per request, not multiple.");
                }

                file ??= Request.Form.Files[0];

                if (file.ContentDisposition.IsEmpty())
                {
                    return BadRequest("Missing file parameters in content-disposition header.");
                }

                // Content disposition header values take precedence over form values.
                var cd = ContentDispositionHeaderValue.Parse(file.ContentDisposition);

                isTransient = cd.GetParameterValue("isTransient", isTransient);
                path = cd.GetParameterValue("path", path.NullEmpty() ?? $"{SystemAlbumProvider.Files}/{Path.GetFileName(file.FileName)}");

                var rawDuplicateFileHandling = cd.GetParameterValue<string>("duplicateFileHandling");
                if (Enum.TryParse<DuplicateFileHandling>(rawDuplicateFileHandling.EmptyNull(), out var tmp))
                {
                    duplicateFileHandling = tmp;
                }

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
