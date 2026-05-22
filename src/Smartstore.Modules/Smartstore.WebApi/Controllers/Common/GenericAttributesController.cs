using Microsoft.OData;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Api.Controllers;

/// <summary>
/// The endpoint for operations on GenericAttribute entity.
/// </summary>
/// <remarks>
/// Most of the key "Generic Attributes" are assigned to customers. Therefore, permissions are checked at the customer level
/// to ensure that only authorized users have access to these attributes.
/// </remarks>
[WebApiGroup(WebApiGroupNames.Common)]
public class GenericAttributesController : WebApiController<GenericAttribute>
{
    private readonly string[] _forbiddenCustomerAttributes =
    [
        WebApiService.AttributeUserDataKey,
        SystemCustomerAttributeNames.PasswordRecoveryToken,
        SystemCustomerAttributeNames.AccountActivationToken,
        SystemCustomerAttributeNames.ImpersonatedCustomerId
    ];

    [HttpGet("GenericAttributes"), ApiQueryable]
    [Permission(Permissions.Customer.Read)]
    public IQueryable<GenericAttribute> Get()
    {
        return Entities.AsNoTracking();
    }

    [HttpGet("GenericAttributes({key})"), ApiQueryable]
    [Permission(Permissions.Customer.Read)]
    public SingleResult<GenericAttribute> Get(int key)
    {
        return GetById(key);
    }

    [HttpPost]
    [Permission(Permissions.Customer.Create)]
    [ProducesResponseType(Status403Forbidden)]
    public async Task<IActionResult> Post([FromBody] GenericAttribute entity)
    {
        return await PostAsync(entity, async () =>
        {
            CheckCustomerAttributes(entity);
            await Db.SaveChangesAsync();
        });
    }

    [HttpPut]
    [Permission(Permissions.Customer.Update)]
    [ProducesResponseType(Status403Forbidden)]
    public Task<IActionResult> Put(int key, Delta<GenericAttribute> model)
    {
        return PutAsync(key, model, async (entity) =>
        {
            CheckCustomerAttributes(entity);
            await Db.SaveChangesAsync();
        });
    }

    [HttpPatch]
    [Permission(Permissions.Customer.Update)]
    [ProducesResponseType(Status403Forbidden)]
    public Task<IActionResult> Patch(int key, Delta<GenericAttribute> model)
    {
        return PatchAsync(key, model, async (entity) =>
        {
            CheckCustomerAttributes(entity);
            await Db.SaveChangesAsync();
        });
    }

    [HttpDelete]
    [Permission(Permissions.Customer.Delete)]
    [ProducesResponseType(Status403Forbidden)]
    public Task<IActionResult> Delete(int key)
    {
        return DeleteAsync(key, async (entity) =>
        {
            CheckCustomerAttributes(entity);
            await Db.SaveChangesAsync();
        });
    }

    private void CheckCustomerAttributes(GenericAttribute entity)
    {
        if (entity != null
            && entity.KeyGroup.EqualsNoCase(nameof(Customer))
            && _forbiddenCustomerAttributes.Contains(entity.Key, StringComparer.OrdinalIgnoreCase))
        {
            throw new ODataErrorException(ODataHelper.CreateError($"It is not allowed to add, modify or delete a generic attribute of key '{entity.Key}'.", Status403Forbidden));
        }
    }
}