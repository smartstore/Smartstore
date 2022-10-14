using System.Net;
using Microsoft.AspNetCore.Identity;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Api.Controllers.OData
{
    public class CustomersController : WebApiController<Customer>
    {
        private readonly Lazy<UserManager<Customer>> _userManager;

        public CustomersController(Lazy<UserManager<Customer>> userManager)
        {
            _userManager = userManager;
        }

        //[NonAction]
        //public static void Init(ODataModelBuilder builder)
        //{
        //    builder.EntitySet<Customer>("Customers");

        //    var type = builder.EntityType<Customer>();

        //    var action = type.Collection.Action(nameof(Post));
        //    action.Parameter<string>("Password");//.Required();
        //}

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public IQueryable<Customer> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Customer> Get(int key)
        {
            return GetById(key);
        }

        // TODO addresses

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Address> GetBillingAddress(int key)
        {
            return GetRelatedEntity(key, x => x.BillingAddress);
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Address> GetShippingAddress(int key)
        {
            return GetRelatedEntity(key, x => x.ShippingAddress);
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<Order> GetOrders(int key)
        {
            return GetRelatedQuery(key, x => x.Orders);
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Order.ReturnRequest.Read)]
        public IQueryable<ReturnRequest> GetReturnRequests(int key)
        {
            return GetRelatedQuery(key, x => x.ReturnRequests);
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Customer.Role.Read)]
        public IQueryable<CustomerRoleMapping> GetCustomerRoleMappings(int key)
        {
            return GetRelatedQuery(key, x => x.CustomerRoleMappings);
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<RewardPointsHistory> GetRewardPointsHistory(int key)
        {
            return GetRelatedQuery(key, x => x.RewardPointsHistory);
        }

        // TODO: (mg) (core) test POST Customers. Throws model binding error at the moment.
        [HttpPost]
        [Permission(Permissions.Customer.Create)]
        public async Task<IActionResult> Post([FromBody] Customer entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (entity == null)
            {
                return BadRequest($"Missing or invalid API request body for {nameof(Customer)} entity.");
            }

            entity = await ApplyRelatedEntityIdsAsync(entity);

            // TODO: (mg) (core) get password from somewhere
            string password = null;

            var result = await _userManager.Value.CreateAsync(entity, password);
            if (result.Succeeded)
            {
                return Created(entity);
            }
            else
            {
                return UnprocessableEntity(string.Join(" ", result.Errors.Select(x => $"{x.Code} {x.Description}.")));
            }
        }

        [HttpPut]
        [Permission(Permissions.Customer.Update)]
        public async Task<IActionResult> Put(int key, Delta<Customer> model)
        {
            return await PutAsync(key, model, async (entity) =>
            {
                CheckCustomer(entity);

                var result = await _userManager.Value.UpdateAsync(entity);
                if (!result.Succeeded)
                {
                    throw new UnprocessableRequestException(string.Join(" ", result.Errors.Select(x => $"{x.Code} {x.Description}.")));
                }
            });
        }

        [HttpPatch]
        [Permission(Permissions.Customer.Update)]
        public async Task<IActionResult> Patch(int key, Delta<Customer> model)
        {
            return await PatchAsync(key, model, async (entity) =>
            {
                CheckCustomer(entity);

                var result = await _userManager.Value.UpdateAsync(entity);
                if (!result.Succeeded)
                {
                    throw new UnprocessableRequestException(string.Join(" ", result.Errors.Select(x => $"{x.Code} {x.Description}.")));
                }
            });
        }

        [HttpDelete]
        [Permission(Permissions.Customer.Delete)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key, async (entity) =>
            {
                CheckCustomer(entity);

                Db.Customers.Remove(entity);

                if (entity.Email.HasValue())
                {
                    var subscriptions = await Db.NewsletterSubscriptions.Where(x => x.Email == entity.Email).ToListAsync();
                    Db.NewsletterSubscriptions.RemoveRange(subscriptions);
                }

                await Db.SaveChangesAsync();
            });
        }

        private static void CheckCustomer(Customer entity)
        {
            if (entity != null && entity.IsSystemAccount)
            {
                throw new UnprocessableRequestException("Modifying or deleting a system customer account is not allowed.", HttpStatusCode.Forbidden);
            }
        }
    }
}
