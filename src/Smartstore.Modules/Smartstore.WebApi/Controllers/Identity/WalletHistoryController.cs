using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on WalletHistory entity.
    /// </summary>
    public class WalletHistoryController : WebApiController<WalletHistory>
    {
        [HttpGet("WalletHistory"), ApiQueryable]
        [Permission("Wallet.read")]
        public IQueryable<WalletHistory> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("WalletHistory({key})"), ApiQueryable]
        [Permission("Wallet.read")]
        public SingleResult<WalletHistory> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("WalletHistory({key})/Customer"), ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Customer> GetCustomer(int key)
        {
            return GetRelatedEntity(key, x => x.Customer);
        }

        [HttpGet("WalletHistory({key})/Order"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Order> GetOrder(int key)
        {
            return GetRelatedEntity(key, x => x.Order);
        }

        [HttpPost]
        [Permission("Wallet.create")]
        public Task<IActionResult> Post([FromBody] WalletHistory model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission("Wallet.update")]
        public Task<IActionResult> Put(int key, Delta<WalletHistory> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission("Wallet.update")]
        public Task<IActionResult> Patch(int key, Delta<WalletHistory> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission("Wallet.delete")]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
