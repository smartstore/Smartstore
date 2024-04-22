using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductCategory entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductCategoriesController : WebApiController<ProductCategory>
    {
        [HttpGet("ProductCategories"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductCategory> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductCategories({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductCategory> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ProductCategories({key})/Category"), ApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        public SingleResult<Category> GetCategory(int key)
        {
            return GetRelatedEntity(key, x => x.Category);
        }

        [HttpGet("ProductCategories({key})/Product"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<Product> GetProduct(int key)
        {
            return GetRelatedEntity(key, x => x.Product);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        public Task<IActionResult> Post([FromBody] ProductCategory model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        public Task<IActionResult> Put(int key, Delta<ProductCategory> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        public Task<IActionResult> Patch(int key, Delta<ProductCategory> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
