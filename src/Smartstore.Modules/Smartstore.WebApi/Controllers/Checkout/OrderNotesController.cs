using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Messaging;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on OrderNote entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class OrderNotesController : WebApiController<OrderNote>
    {
        private readonly Lazy<IMessageFactory> _messageFactory;

        public OrderNotesController(Lazy<IMessageFactory> messageFactory)
        {
            _messageFactory = messageFactory;
        }

        [HttpGet("OrderNotes"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<OrderNote> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("OrderNotes({key})"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<OrderNote> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("OrderNotes({key})/Order"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Order> GetOrder(int key)
        {
            return GetRelatedEntity(key, x => x.Order);
        }

        /// <param name="notifyCustomer">
        /// A value indicating whether to send a notification to the customer about the new order note.
        /// Only applicable if **DisplayToCustomer** is true.
        /// </param>
        [HttpPost]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> Post(
            [FromBody] OrderNote entity,
            [FromQuery] bool notifyCustomer = false)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (entity == null)
            {
                return BadRequest($"Missing or invalid API request body for {nameof(OrderNote)} entity.");
            }
            if (entity.Note.IsEmpty())
            {
                return BadRequest("Missing or empty order note text.");
            }

            var order = await Db.Orders
                .Include(x => x.Customer)
                .FindByIdAsync(entity.OrderId);
            if (order == null)
            {
                return NotFound(entity.OrderId, nameof(Order));
            }

            try
            {
                order.OrderNotes.Add(entity);
                await Db.SaveChangesAsync();

                if (entity.DisplayToCustomer && notifyCustomer)
                {
                    await _messageFactory.Value.SendNewOrderNoteAddedCustomerNotificationAsync(entity, order.CustomerLanguageId);
                }

                return Created(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        [HttpPut]
        [Permission(Permissions.Order.Update)]
        public Task<IActionResult> Put(int key, Delta<OrderNote> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Order.Update)]
        public Task<IActionResult> Patch(int key, Delta<OrderNote> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Order.Update)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
