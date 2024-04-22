using Smartstore.Core.Checkout.Payment;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on RecurringPaymentHistory entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class RecurringPaymentHistoryController : WebApiController<RecurringPaymentHistory>
    {
        [HttpGet("RecurringPaymentHistory"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<RecurringPaymentHistory> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("RecurringPaymentHistory({key})"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<RecurringPaymentHistory> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("RecurringPaymentHistory({key})/RecurringPayment"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<RecurringPayment> GetRecurringPayment(int key)
        {
            return GetRelatedEntity(key, x => x.RecurringPayment);
        }

        [HttpPost]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public Task<IActionResult> Post([FromBody] RecurringPaymentHistory model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public Task<IActionResult> Put(int key, Delta<RecurringPaymentHistory> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public Task<IActionResult> Patch(int key, Delta<RecurringPaymentHistory> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
