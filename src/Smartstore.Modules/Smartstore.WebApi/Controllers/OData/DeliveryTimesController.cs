using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Web.Api.Models.OData;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on DeliveryTime entity.
    /// </summary>
    public class DeliveryTimesController : WebApiController<DeliveryTime>
    {
        private readonly Lazy<IDeliveryTimeService> _deliveryTimeService;

        public DeliveryTimesController(Lazy<IDeliveryTimeService> deliveryTimeService)
        {
            _deliveryTimeService = deliveryTimeService;
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.DeliveryTime.Read)]
        public IQueryable<DeliveryTime> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.DeliveryTime.Read)]
        public SingleResult<DeliveryTime> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.DeliveryTime.Create)]
        public async Task<IActionResult> Post([FromBody] DeliveryTime entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.DeliveryTime.Update)]
        public async Task<IActionResult> Put(int key, Delta<DeliveryTime> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.DeliveryTime.Update)]
        public async Task<IActionResult> Patch(int key, Delta<DeliveryTime> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.DeliveryTime.Delete)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }

        #region Actions and functions

        /// <summary>
        /// Gets the calculated minimum and maximum delivery date (if any).
        /// </summary>
        [HttpGet("DeliveryTimes/GetDeliveryDate(Id={id})")]
        [Permission(Permissions.Configuration.DeliveryTime.Read)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SimpleRange<DateTime?>), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> GetDeliveryDate(int id)
        {
            var deliveryTime = await Db.DeliveryTimes.FindByIdAsync(id, false);
            if (deliveryTime == null)
            {
                return NotFound(id);
            }

            var (min, max) = _deliveryTimeService.Value.GetDeliveryDate(deliveryTime);

            var result = new SimpleRange<DateTime?>
            {
                Minimum = min,
                Maximum = max
            };

            return Ok(result);
        }

        #endregion
    }
}
