using Smartstore.Core.Seo;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on UrlRecord entity.
    /// </summary>
    public class UrlRecordsController : WebApiController<UrlRecord>
    {
        [HttpGet("UrlRecords"), ApiQueryable]
        public IQueryable<UrlRecord> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("UrlRecords({key})"), ApiQueryable]
        public SingleResult<UrlRecord> Get(int key)
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
    }
}
