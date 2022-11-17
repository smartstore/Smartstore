using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Product entity.
    /// </summary>
    public class ProductsController : WebApiController<Product>
    {
        private readonly Lazy<IUrlService> _urlService;

        public ProductsController(Lazy<IUrlService> urlService)
        {
            _urlService = urlService;
        }

        // INFO: unlike in Classic, also return system products. Someone may well use them for their own purposes.

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<Product> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<Product> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Create)]
        public Task<IActionResult> Post([FromBody] Product entity)
        {
            return PostAsync(entity, async () =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.Update)]
        public Task<IActionResult> Put(int key, Delta<Product> model)
        {
            return PutAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.Update)]
        public Task<IActionResult> Patch(int key, Delta<Product> model)
        {
            return PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }



        private async Task UpdateSlug(Product entity)
        {
            var slugResult = await _urlService.Value.ValidateSlugAsync(entity, string.Empty, true);
            await _urlService.Value.ApplySlugAsync(slugResult, true);
        }
    }
}
