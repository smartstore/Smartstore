using Microsoft.AspNetCore.Identity;
using Microsoft.OData;
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

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public IQueryable<Address> GetAddresses(int key)
        {
            return GetRelatedQuery(key, x => x.Addresses);
        }

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
                throw new ODataErrorException(CreateError(result));
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
                    throw new ODataErrorException(CreateError(result));
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
                    throw new ODataErrorException(CreateError(result));
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

        /// <summary>
        /// Assigns an Address to a Customer.
        /// </summary>
        /// <remarks>
        /// The assignment is created only if it does not already exist.
        /// </remarks>
        /// <param name="relatedkey">The address identifier.</param>
        [HttpPost("Customers({key})/Addresses({relatedkey})")]
        [HttpPost("Customers/{key}/Addresses/{relatedkey}")]
        [Permission(Permissions.Customer.EditAddress)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Address), Status200OK)]
        [ProducesResponseType(typeof(Address), Status201Created)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> PostAddresses(int key, int relatedkey)
        {
            var entity = await Entities
                .Include(x => x.Addresses)
                .FindByIdAsync(key);

            var address = entity.Addresses.FirstOrDefault(x => x.Id == relatedkey);
            if (address == null)
            {
                // No assignment yet.
                address = await Db.Addresses.FindByIdAsync(relatedkey, false);
                if (address == null)
                {
                    return NotFound($"Cannot find Address entity with identifier {relatedkey}.");
                }

                entity.Addresses.Add(address);
                await Db.SaveChangesAsync();

                return Created(address);
            }

            return Ok(address);
        }

        /// <summary>
        /// Removes the assignment of an Address to a Customer.
        /// </summary>
        /// <param name="relatedkey">The address identifier. 0 to remove all address assignments.</param>
        [HttpDelete("Customers({key})/Addresses({relatedkey})")]
        [HttpDelete("Customers/{key}/Addresses/{relatedkey}")]
        [Permission(Permissions.Customer.EditAddress)]
        [ProducesResponseType(Status204NoContent)]
        public async Task<IActionResult> DeleteAddresses(int key, int relatedkey)
        {
            var entity = await Entities
                .Include(x => x.Addresses)
                .FindByIdAsync(key);

            if (relatedkey == 0)
            {
                // Remove assignments of all addresses.
                entity.BillingAddress = null;
                entity.ShippingAddress = null;
                entity.Addresses.Clear();
                await Db.SaveChangesAsync();
            }
            else
            {
                // Remove assignment of certain address.
                var address = await Db.Addresses.FindByIdAsync(relatedkey);
                if (address != null)
                {
                    entity.RemoveAddress(address);
                    await Db.SaveChangesAsync();
                }
            }

            return NoContent();
        }

        //[HttpPost, HttpPut]
        //[Permission(Permissions.Customer.EditAddress)]
        //public IActionResult CreateRef(int key, string navigationProperty, [FromBody] Uri link)
        //{
        //    var relatedKey = GetRelatedKey(link);
        //    $"CreateRef. key:{key} navigationProperty:{navigationProperty}  relatedKey:{relatedKey} link:{link}".Dump();

        //    return NoContent();
        //}

        private static void CheckCustomer(Customer entity)
        {
            if (entity != null && entity.IsSystemAccount)
            {
                throw new ODataErrorException(new ODataError
                {
                    ErrorCode = StatusCodes.Status403Forbidden.ToString(),
                    Message = "Modifying or deleting a system customer account is not allowed."
                });
            }
        }

        private static ODataError CreateError(IdentityResult result)
        {
            return new()
            {
                ErrorCode = StatusCodes.Status422UnprocessableEntity.ToString(),
                Message = result.ToString(),
                Details = result.Errors.Select(x => new ODataErrorDetail
                {
                    ErrorCode = x.Code,
                    Message = x.Description
                })
                .ToList()
            };
        }
    }
}
