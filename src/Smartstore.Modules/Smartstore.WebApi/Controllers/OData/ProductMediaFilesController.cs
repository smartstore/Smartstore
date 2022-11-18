using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on ProductMediaFile entity.
    /// </summary>
    public class ProductMediaFilesController : WebApiController<ProductMediaFile>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductMediaFile> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductMediaFile> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<MediaFile> GetMediaFile(int key)
        {
            return GetRelatedEntity(key, x => x.MediaFile);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<Product> GetProduct(int key)
        {
            return GetRelatedEntity(key, x => x.Product);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        public Task<IActionResult> Post([FromBody] ProductMediaFile model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        public Task<IActionResult> Put(int key, Delta<ProductMediaFile> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        public Task<IActionResult> Patch(int key, Delta<ProductMediaFile> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
