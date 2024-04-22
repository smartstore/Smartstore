using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on RecurringPayment entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class RecurringPaymentsController : WebApiController<RecurringPayment>
    {
        private readonly Lazy<IOrderProcessingService> _orderProcessingService;

        public RecurringPaymentsController(Lazy<IOrderProcessingService> orderProcessingService)
        {
            _orderProcessingService = orderProcessingService;
        }

        [HttpGet("RecurringPayments"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<RecurringPayment> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("RecurringPayments({key})"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<RecurringPayment> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("RecurringPayments({key})/RecurringPaymentHistory"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<RecurringPaymentHistory> GetRecurringPaymentHistory(int key)
        {
            return GetRelatedQuery(key, x => x.RecurringPaymentHistory);
        }

        [HttpGet("RecurringPayments({key})/InitialOrder"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Order> GetInitialOrder(int key)
        {
            return GetRelatedEntity(key, x => x.InitialOrder);
        }

        [HttpPost]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public Task<IActionResult> Post([FromBody] RecurringPayment model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public Task<IActionResult> Put(int key, Delta<RecurringPayment> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public Task<IActionResult> Patch(int key, Delta<RecurringPayment> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }

        #region Actions and functions

        /// <summary>
        /// Processes the next recurring payment.
        /// </summary>
        [HttpPost("RecurringPayments({key})/ProcessNextRecurringPayment"), ApiQueryable]
        [Permission(Permissions.Order.EditRecurringPayment)]
        [Produces(Json)]
        [ProducesResponseType(typeof(RecurringPayment), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> ProcessNextRecurringPayment(int key)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q
                    .Include(x => x.InitialOrder)
                    .ThenInclude(x => x.Customer));

                await _orderProcessingService.Value.ProcessNextRecurringPaymentAsync(entity);

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Cancels a recurring payment.
        /// </summary>
        [HttpPost("RecurringPayments({key})/CancelRecurringPayment"), ApiQueryable]
        [Permission(Permissions.Order.EditRecurringPayment)]
        [Produces(Json)]
        [ProducesResponseType(typeof(RecurringPayment), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> CancelRecurringPayment(int key)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q
                    .Include(x => x.InitialOrder)
                    .ThenInclude(x => x.Customer));

                await _orderProcessingService.Value.CancelRecurringPaymentAsync(entity);

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
