using System.Dynamic;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Api.Models;
using Smartstore.Web.Api.Models.Checkout;

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
        /// Provides information on payment providers.
        /// </summary>
        /// <param name="active" example="true">A value indicating whether to only include active payment methods. **False** to get information about all payment methods.</param>
        /// <param name="storeId">Filter payment methods by store identifier. 0 to get all.</param>
        /// <param name="languageId">The ID of the language in which localizable information is returned. Obtained form working language if 0.</param>
        [HttpGet("PaymentMethods/GetAllPaymentMethods(active={active},storeId={storeId},languageId={languageId})")]
        [Permission(Permissions.Configuration.PaymentMethod.Read)]
        [Produces(Json)]
        [ProducesResponseType(typeof(IEnumerable<ProviderInfo<PaymentMethodInfo>>), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> GetAllPaymentMethods(bool active, int storeId = 0, int languageId = 0)
        {
            try
            {
                var providers = await _paymentService.Value.LoadAllPaymentProvidersAsync(active, storeId);
                var mapper = MapperFactory.GetMapper<Provider<IPaymentMethod>, ProviderInfo<PaymentMethodInfo>>();
                dynamic parameters = new ExpandoObject();
                parameters.LanguageId = languageId;

                var infos = await providers
                    .SelectAwait(async x =>
                    {
                        var model = new ProviderInfo<PaymentMethodInfo>();
                        await mapper.MapAsync(x, model, parameters);
                        return model;
                    })
                    .AsyncToList();

                return Ok(infos);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
