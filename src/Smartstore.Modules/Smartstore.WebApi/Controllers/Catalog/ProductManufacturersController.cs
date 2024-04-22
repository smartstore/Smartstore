using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductManufacturer entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductManufacturersController : WebApiController<ProductManufacturer>
    {
        [HttpGet("ProductManufacturers"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductManufacturer> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductManufacturers({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductManufacturer> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ProductManufacturers({key})/Manufacturer"), ApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public SingleResult<Manufacturer> GetManufacturer(int key)
        {
            return GetRelatedEntity(key, x => x.Manufacturer);
        }

        [HttpGet("ProductManufacturers({key})/Product"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<Product> GetProduct(int key)
        {
            return GetRelatedEntity(key, x => x.Product);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditManufacturer)]
        public Task<IActionResult> Post([FromBody] ProductManufacturer model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditManufacturer)]
        public Task<IActionResult> Put(int key, Delta<ProductManufacturer> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditManufacturer)]
        public Task<IActionResult> Patch(int key, Delta<ProductManufacturer> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.EditManufacturer)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
