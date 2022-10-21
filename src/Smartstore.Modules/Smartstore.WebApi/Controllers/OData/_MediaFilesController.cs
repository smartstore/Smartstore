using System.Globalization;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
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

        public MediaFilesController(IMediaService mediaService)
        {
            _mediaService = mediaService;
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
                return NotFound($"Cannot find {nameof(MediaFile)} entity with identifier {key}.");
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

        /// <summary>
        /// Gets a file by path.
        /// </summary>
        /// <example>
        /// POST /MediaFiles/GetFileByPath {"Path":"content/my-file.jpg"}
        /// </example>
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(FileItemInfo), Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        [HttpPost, WebApiQueryable]
        public async Task<IActionResult> GetFileByPath(ODataActionParameters parameters, ODataQueryOptions<MediaFile> options)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

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

        private void DeleteFile()
        {
            throw new NotImplementedException();
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
