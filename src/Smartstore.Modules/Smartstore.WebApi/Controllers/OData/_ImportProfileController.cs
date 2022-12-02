using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using Smartstore.Core.DataExchange;
using Smartstore.IO;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on ImportProfile entity.
    /// </summary>
    public class ImportProfileController : WebApiController<ImportProfile>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Configuration.Import.Read)]
        public IQueryable<ImportProfile> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Configuration.Import.Read)]
        public SingleResult<ImportProfile> Get(int key)
        {
            return GetById(key);
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
            return Forbidden();
        }

        #region Actions and functions

        /// <summary>
        /// Saves import files and assigns them to an ImportProfile.
        /// </summary>
        /// <param name="key">
        /// Identifier of the import profile to which the files should be assigned.
        /// 0 if the profile is to be identified by name.
        /// </param>
        /// <param name="files">The files to be saved.</param>
        /// <param name="name">Optional name by which the import profile is to be identified.</param>
        /// <param name="deleteFiles">A value indicating whether to delete existing files of the profile.</param>
        /// <param name="startImport">A value indicating whether the import should be started.</param>
        [HttpPost("ImportProfiles({key})/SaveFiles"), ApiQueryable]
        [Permission(Permissions.Configuration.Import.Execute)]
        [Consumes("multipart/form-data"), Produces(Json)]
        [ProducesResponseType(typeof(ImportProfile), Status200OK)]
        [ProducesResponseType(Status415UnsupportedMediaType)]
        public async Task<IActionResult> SaveFiles(int key,
            [Required] IFormFileCollection files,
            [FromQuery] string name = null,
            [FromForm] bool deleteFiles = false,
            [FromForm] bool startImport = false)
        {
            if (!HasMultipartContent)
            {
                return StatusCode(Status415UnsupportedMediaType);
            }

            try
            {
                files = Request.Form.Files;
                if (files.Count == 0)
                {
                    return BadRequest("Missing multipart file data.");
                }

                var entity = (ImportProfile)null;

                if (key != 0)
                {
                    entity = await GetRequiredById(key);
                }
                else if (name.HasValue())
                {
                    entity = await Entities.FirstOrDefaultAsync(x => x.Name == name);
                }

                if (entity == null)
                {
                    return NotFound($"Cannot find {nameof(ImportProfile)} entity. Please specify a valid ID or profile name.");
                }

                foreach (var file in files)
                {
                    if (file.FileName.IsEmpty())
                    {
                        return BadRequest("The file name is missing.");
                    }

                    var fileName = PathUtility.SanitizeFileName(file.FileName);
                    var extension = Path.GetExtension(fileName);

                    if (extension.EqualsNoCase(".zip"))
                    {
                        using var stream = file.OpenReadStream();
                        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

                        foreach (var entry in archive.Entries)
                        {
                        }
                    }
                    else
                    {
                    }
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
