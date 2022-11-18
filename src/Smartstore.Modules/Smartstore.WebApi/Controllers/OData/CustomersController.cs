using Microsoft.AspNetCore.Identity;
using Microsoft.OData;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Customer entity.
    /// </summary>
    public class CustomersController : WebApiController<Customer>
    {
        private readonly Lazy<UserManager<Customer>> _userManager;

        public CustomersController(Lazy<UserManager<Customer>> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public IQueryable<Customer> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Customer> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public IQueryable<Address> GetAddresses(int key)
        {
            return GetRelatedQuery(key, x => x.Addresses);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Address> GetBillingAddress(int key)
        {
            return GetRelatedEntity(key, x => x.BillingAddress);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Address> GetShippingAddress(int key)
        {
            return GetRelatedEntity(key, x => x.ShippingAddress);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<Order> GetOrders(int key)
        {
            return GetRelatedQuery(key, x => x.Orders);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.ReturnRequest.Read)]
        public IQueryable<ReturnRequest> GetReturnRequests(int key)
        {
            return GetRelatedQuery(key, x => x.ReturnRequests);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Role.Read)]
        public IQueryable<CustomerRoleMapping> GetCustomerRoleMappings(int key)
        {
            return GetRelatedQuery(key, x => x.CustomerRoleMappings);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<RewardPointsHistory> GetRewardPointsHistory(int key)
        {
            return GetRelatedQuery(key, x => x.RewardPointsHistory);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Create)]
        public async Task<IActionResult> Post([FromBody] Customer model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (model == null)
            {
                return BadRequest($"Missing or invalid API request body for {nameof(Customer)} entity.");
            }

            model = await ApplyRelatedEntityIdsAsync(model);

            var result = await _userManager.Value.CreateAsync(model);
            if (result.Succeeded)
            {
                return Created(model);
            }
            else
            {
                throw new ODataErrorException(CreateError(result));
            }
        }

        [HttpPut]
        [Permission(Permissions.Customer.Update)]
        [ProducesResponseType(Status403Forbidden)]
        public Task<IActionResult> Put(int key, Delta<Customer> model)
        {
            return PutAsync(key, model, async (entity) =>
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
        [ProducesResponseType(Status403Forbidden)]
        public Task<IActionResult> Patch(int key, Delta<Customer> model)
        {
            return PatchAsync(key, model, async (entity) =>
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
        [ProducesResponseType(Status403Forbidden)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key, async (entity) =>
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
        /// <param name="relatedkey">The Address identifier.</param>
        [HttpPost("Customers({key})/Addresses({relatedkey})")]
        [Permission(Permissions.Customer.EditAddress)]
        [Produces(Json)]
        [ProducesResponseType(typeof(Address), Status200OK)]
        [ProducesResponseType(typeof(Address), Status201Created)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> PostAddresses(int key, int relatedkey)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.Addresses));
                var address = entity.Addresses.FirstOrDefault(x => x.Id == relatedkey);
                if (address == null)
                {
                    // No assignment yet.
                    address = await Db.Addresses.FindByIdAsync(relatedkey, false);
                    if (address == null)
                    {
                        return NotFound(relatedkey, nameof(Address));
                    }

                    entity.Addresses.Add(address);
                    await Db.SaveChangesAsync();

                    return Created(address);
                }

                return Ok(address);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Removes the assignment of an Address to a Customer.
        /// </summary>
        /// <param name="relatedkey">The Address identifier. 0 to remove all address assignments.</param>
        [HttpDelete("Customers({key})/Addresses({relatedkey})")]
        [Permission(Permissions.Customer.EditAddress)]
        [ProducesResponseType(Status204NoContent)]
        public async Task<IActionResult> DeleteAddresses(int key, int relatedkey)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.Addresses));

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
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        // CreateRef works:
        //[HttpPost]
        //public async Task<IActionResult> CreateRefToAddresses(int key, [FromBody] Uri link)
        //{
        //    var relatedKey = GetRelatedKey<int>(link);

        //    var entity = await Entities
        //        .Include(x => x.Addresses)
        //        .FindByIdAsync(key);

        //    var address = entity.Addresses.FirstOrDefault(x => x.Id == relatedKey);
        //    if (address == null)
        //    {
        //        // No assignment yet.
        //        address = await Db.Addresses.FindByIdAsync(relatedKey, false);
        //        if (address == null)
        //        {
        //            return NotFound(relatedkey, nameof(Address));
        //        }

        //        entity.Addresses.Add(address);
        //        await Db.SaveChangesAsync();

        //        return Created(address);
        //    }

        //    return Ok(address);
        //}

        // DeleteRef does not work. Method is never found. Looks like a bug in .Net Core 6:
        // https://stackoverflow.com/questions/73451347/odata-controller-get-and-post-actions-for-many-to-many-entity
        //[HttpDelete]
        //public IActionResult DeleteRefToAddresses([FromODataUri] int key, [FromODataUri] string relatedKey)
        //{
        //    $"DeleteRefToAddresses. key:{key} relatedKey:{relatedKey}".Dump();
        //    return NoContent();
        //}

        private static void CheckCustomer(Customer entity)
        {
            if (entity != null && entity.IsSystemAccount)
            {
                throw new ODataErrorException(new ODataError
                {
                    ErrorCode = Status403Forbidden.ToString(),
                    Message = "Modifying or deleting a system customer account is not allowed."
                });
            }
        }

        private static ODataError CreateError(IdentityResult result)
        {
            return new()
            {
                ErrorCode = Status422UnprocessableEntity.ToString(),
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
