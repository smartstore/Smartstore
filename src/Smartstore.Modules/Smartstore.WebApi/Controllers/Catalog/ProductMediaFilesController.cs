using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductMediaFile entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductMediaFilesController : WebApiController<ProductMediaFile>
    {
        [HttpGet("ProductMediaFiles"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductMediaFile> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductMediaFiles({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductMediaFile> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ProductMediaFiles({key})/MediaFile"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<MediaFile> GetMediaFile(int key)
        {
            return GetRelatedEntity(key, x => x.MediaFile);
        }

        [HttpGet("ProductMediaFiles({key})/Product"), ApiQueryable]
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
