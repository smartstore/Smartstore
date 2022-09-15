using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.Web.Api.Controllers.OData
{
    // TODO: (mg) (core) IEEE754Compatible=true is not supported\working.
    // https://github.com/OData/odata.net/commit/e0e628a495942fb3b6b8acfa62ddbf75d24a1ece
    public class DiscountsController : SmartODataController<Discount>
    {
        [HttpGet, WebApiQueryable]
        [ProducesResponseType(typeof(IEnumerable<Discount>), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        public IActionResult Get()
        {
            return Ok(Entities);
        }

        [HttpGet, WebApiQueryable]
        [ProducesResponseType(typeof(Discount), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Get(int key)
        {
            return GetByKeyAsync(key);
        }

        //....
    }
}
