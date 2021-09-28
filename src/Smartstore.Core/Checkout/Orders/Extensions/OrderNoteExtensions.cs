using Smartstore.Core.Checkout.Orders;
using Smartstore.Utilities.Html;

namespace Smartstore
{
    public static partial class OrderNoteExtensions
    {
        /// <summary>
        /// Formats the order note text.
        /// </summary>
        /// <param name="orderNote">Order note.</param>
        /// <returns>Formatted text.</returns>
        public static string FormatOrderNoteText(this OrderNote orderNote)
        {
            Guard.NotNull(orderNote, nameof(orderNote));

            if (orderNote.Note.IsEmpty())
            {
                return string.Empty;
            }

            return HtmlUtility.ConvertPlainTextToHtml(orderNote.Note);
        }
    }
}
