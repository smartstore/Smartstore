using System.Globalization;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.ModelBuilder;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Api.Models.OData.Media;

namespace Smartstore.Web.Api.Controllers.OData
{
    // INFO: inheritance from WebApiController<MediaFile> does not work. FileItemInfo.File never gets expanded.

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

        public static void Init(ODataModelBuilder builder)
        {
            //var fileSet = builder.EntitySet<MediaFile>("MediaFiles");
            //var infoSet = builder.EntitySet<FileItemInfo>("FileItemInfos");

            var fileSet = builder.EntitySet<MediaFile>("MediaFileEntities");
            var infoSet = builder.EntitySet<FileItemInfo>("MediaFiles");

            builder.EntitySet<MediaFolder>("MediaFolderEntities");
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
                return NotFound();
            }

            return Ok(Convert(file));
        }




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
