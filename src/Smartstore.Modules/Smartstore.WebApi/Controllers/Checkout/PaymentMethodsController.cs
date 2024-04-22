using Smartstore.Core.Checkout.Payment;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on PaymentMethod entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class PaymentMethodsController : WebApiController<PaymentMethod>
    {
        private readonly Lazy<IPaymentService> _paymentService;

        public PaymentMethodsController(Lazy<IPaymentService> paymentService)
        {
            _paymentService = paymentService;
        }

        /// <remarks>
        /// Since payment methods are based on providers, a **PaymentMethod** entity does not necessarily have to exist for each method.
        /// Only **GetAllPaymentMethods** returns a complete list of all payment method names because it queries the providers.
        /// </remarks>
        [HttpGet("PaymentMethods"), ApiQueryable]
        [Permission(Permissions.Configuration.PaymentMethod.Read)]
        public IQueryable<PaymentMethod> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("PaymentMethods({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.PaymentMethod.Read)]
        public SingleResult<PaymentMethod> Get(int key)
        {
            return GetById(key);
        }

        // INFO: update permission is sufficient here.

        [HttpPost]
        [Permission(Permissions.Configuration.PaymentMethod.Update)]
        public Task<IActionResult> Post([FromBody] PaymentMethod model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.PaymentMethod.Update)]
        public Task<IActionResult> Put(int key, Delta<PaymentMethod> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.PaymentMethod.Update)]
        public Task<IActionResult> Patch(int key, Delta<PaymentMethod> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Delete()
        {
            return Forbidden();
        }

        #region Actions and functions

        /// <summary>
        /// Gets the system name of all payment providers.
        /// </summary>
        /// <param name="active" example="true">A value indicating whether to only include active payment methods. **False** to load all payment method names.</param>
        /// <param name="storeId">Filter payment methods by store identifier. 0 to load all.</param>
        [HttpGet("PaymentMethods/GetAllPaymentMethods(active={active},storeId={storeId})")]
        [Permission(Permissions.Configuration.PaymentMethod.Read)]
        [Produces(Json)]
        [ProducesResponseType(typeof(IEnumerable<string>), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> GetAllPaymentMethods(bool active, int storeId = 0)
        {
            try
            {
                var providers = await _paymentService.Value.LoadAllPaymentProvidersAsync(active, storeId);
                var systemNames = providers.Select(x => x.Metadata.SystemName).ToArray();

                return Ok(systemNames);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
