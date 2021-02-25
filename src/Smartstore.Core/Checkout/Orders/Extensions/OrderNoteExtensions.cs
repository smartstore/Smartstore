using Smartstore.Utilities.Html;

namespace Smartstore.Core.Checkout.Orders
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

            return HtmlUtils.ConvertPlainTextToHtml(orderNote.Note);
        }
    }
}
