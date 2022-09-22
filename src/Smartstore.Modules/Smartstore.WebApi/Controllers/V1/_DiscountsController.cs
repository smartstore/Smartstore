using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.Web.Api.Controllers.V1
{
    // TODO: (mg) (core) IEEE754Compatible=true is not supported\working.
    // https://github.com/OData/odata.net/commit/e0e628a495942fb3b6b8acfa62ddbf75d24a1ece

    /// <summary>
    /// The endpoint for operations on Discount entity.
    /// </summary>
    public class DiscountsController : WebApi1Controller<Discount>
    {
        [HttpGet, WebApiQueryable]
        [ProducesResponseType(typeof(IEnumerable<Discount>), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        public IQueryable<Discount> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        [ProducesResponseType(typeof(Discount), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Get(int key)
        {
            return GetByIdAsync(key);
        }

        //....
    }
}
