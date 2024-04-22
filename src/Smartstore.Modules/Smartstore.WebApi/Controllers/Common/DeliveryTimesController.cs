using Smartstore.Core.Common.Services;
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on DeliveryTime entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class DeliveryTimesController : WebApiController<DeliveryTime>
    {
        private readonly Lazy<IDeliveryTimeService> _deliveryTimeService;

        public DeliveryTimesController(Lazy<IDeliveryTimeService> deliveryTimeService)
        {
            _deliveryTimeService = deliveryTimeService;
        }

        [HttpGet("DeliveryTimes"), ApiQueryable]
        [Permission(Permissions.Configuration.DeliveryTime.Read)]
        public IQueryable<DeliveryTime> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("DeliveryTimes({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.DeliveryTime.Read)]
        public SingleResult<DeliveryTime> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.DeliveryTime.Create)]
        public Task<IActionResult> Post([FromBody] DeliveryTime model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.DeliveryTime.Update)]
        public Task<IActionResult> Put(int key, Delta<DeliveryTime> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.DeliveryTime.Update)]
        public Task<IActionResult> Patch(int key, Delta<DeliveryTime> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.DeliveryTime.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }

        #region Actions and functions

        /// <summary>
        /// Gets the calculated minimum and maximum delivery date (if any).
        /// </summary>
        [HttpGet("DeliveryTimes/GetDeliveryDate(Id={id})")]
        [Permission(Permissions.Configuration.DeliveryTime.Read)]
        [Produces(Json)]
        [ProducesResponseType(typeof(SimpleRange<DateTime?>), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> GetDeliveryDate(int id)
        {
            try
            {
                var entity = await GetRequiredById(id);
                var (min, max) = _deliveryTimeService.Value.GetDeliveryDate(entity);

                var result = new SimpleRange<DateTime?>
                {
                    Minimum = min,
                    Maximum = max
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
