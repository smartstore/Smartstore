using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.WebApi.Controllers.OData
{
    // TODO: (mg) (core) IEEE754Compatible=true is not supported\working.
    public class DiscountsController : SmartODataController<Discount>
    {
        [HttpGet, Queryable]
        [ProducesResponseType(typeof(IEnumerable<Discount>), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        public IActionResult Get()
        {
            return Ok(Entities);
        }

        [HttpGet, Queryable]
        [ProducesResponseType(typeof(Discount), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Get(int key)
        {
            return GetByKeyAsync(key);
        }

        //....
    }
}
