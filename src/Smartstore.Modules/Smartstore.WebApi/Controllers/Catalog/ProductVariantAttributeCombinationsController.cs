using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductVariantAttributeCombination entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductVariantAttributeCombinationsController : WebApiController<ProductVariantAttributeCombination>
    {
        [HttpGet("ProductVariantAttributeCombinations"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductVariantAttributeCombination> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductVariantAttributeCombinations({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductVariantAttributeCombination> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ProductVariantAttributeCombinations({key})/DeliveryTime"), ApiQueryable]
        [Permission(Permissions.Configuration.DeliveryTime.Read)]
        public SingleResult<DeliveryTime> GetDeliveryTime(int key)
        {
            return GetRelatedEntity(key, x => x.DeliveryTime);
        }

        [HttpGet("ProductVariantAttributeCombinations({key})/QuantityUnit"), ApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public SingleResult<QuantityUnit> GetQuantityUnit(int key)
        {
            return GetRelatedEntity(key, x => x.QuantityUnit);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Post([FromBody] ProductVariantAttributeCombination model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Put(int key, Delta<ProductVariantAttributeCombination> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Patch(int key, Delta<ProductVariantAttributeCombination> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
