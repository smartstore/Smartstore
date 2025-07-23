#nullable enable

using Smartstore.Core.Checkout.Orders;

namespace Smartstore
{
    public static class OrderNoteDbSetExtensions
    {
        /// <summary>
        /// Adds a new order note to the specified set. This method does NOT commit to database.
        /// </summary>
        /// <param name="order">The order to add the note to.</param>
        /// <param name="note">The note.</param>
        /// <returns>The newly created <see cref="OrderNote"/> entity, or null if the order or note is null/empty.</returns>
        public static OrderNote? Add(this DbSet<OrderNote> dbSet, Order? order, string? note, bool displayToCustomer = false)
        {
            Guard.NotNull(dbSet);

            if (order == null || note.IsEmpty())
            {
                return null;
            }

            var entity = new OrderNote
            {
                OrderId = order.Id,
                Note = note,
                DisplayToCustomer = displayToCustomer,
                CreatedOnUtc = DateTime.UtcNow
            };

            if (order.IsTransientRecord())
            {
                order.OrderNotes.Add(entity);
            }
            else
            {
                dbSet.Add(entity);
            }     

            return entity;
        }
    }
}
