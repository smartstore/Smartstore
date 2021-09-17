using Smartstore.Forums.Domain;
using Smartstore.Forums.Services;
using Smartstore.Utilities.Html;

namespace Smartstore.Forums
{
    internal static class PrivateMessageExtensions
    {
        /// <summary>
        /// Formats the private message text.
        /// </summary>
        /// <param name="message">Private message.</param>
        /// <returns>Formatted text.</returns>
        public static string FormatPrivateMessageText(this PrivateMessage message)
        {
            Guard.NotNull(message, nameof(message));

            var text = message.Text;
            if (text.IsEmpty())
            {
                return string.Empty;
            }

            text = HtmlUtils.ConvertPlainTextToHtml(text.HtmlEncode());
            return BBCodeHelper.ToHtml(text);
        }
    }
}
