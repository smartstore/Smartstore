using System.ComponentModel.DataAnnotations;
using Smartstore.Core.DataExchange;

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
        /// <param name="files">The files to be saved.</param>
        [HttpPost("ImportProfiles({key})/SaveFiles"), ApiQueryable]
        [Permission(Permissions.Configuration.Import.Execute)]
        [Consumes("multipart/form-data"), Produces(Json)]
        [ProducesResponseType(typeof(ImportProfile), Status200OK)]
        [ProducesResponseType(Status415UnsupportedMediaType)]
        public async Task<IActionResult> SaveFiles(int key,
            [Required] List<IFormFile> files)
        {
            if (!HasMultipartContent)
            {
                return StatusCode(Status415UnsupportedMediaType);
            }

            try
            {
                var formFiles = files.IsNullOrEmpty() ? (IEnumerable<IFormFile>)Request.Form.Files : files;
                if (!formFiles.Any())
                {
                    return BadRequest("Missing multipart file data.");
                }

                var entity = await GetRequiredById(key);

                //...

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
