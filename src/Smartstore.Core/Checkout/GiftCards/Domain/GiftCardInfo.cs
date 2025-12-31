using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Smartstore.Core.Checkout.GiftCards
{
    public interface IGiftCardInfo
    {
        /// <summary>
        /// The name of the recipient.
        /// </summary>
        string RecipientName { get; }

        /// <summary>
        /// The email address of the recipient.
        /// </summary>
        string RecipientEmail { get; }

        /// <summary>
        /// The name of the giver of the gift card.
        /// </summary>
        string SenderName { get; }

        /// <summary>
        /// The email address of the giver of the gift card.
        /// </summary>
        string SenderEmail { get; }

        /// <summary>
        /// An optional message to the recipient.
        /// </summary>
        string Message { get; }
    }

    // INFO: use lower case names when accessing properties of deserialized dynamic GiftCardInfo objects!
    // See ProductVariantAttributeSelection.ToCustomAttributeValue.

    public class GiftCardInfo : IGiftCardInfo
    {
        /// <inheritdoc/>
        [Required]
        [JsonPropertyName("recipientName")]
        public string RecipientName { get; set; }

        /// <inheritdoc/>
        [Required]
        [JsonPropertyName("recipientEmail")]
        public string RecipientEmail { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("senderName")]
        public string SenderName { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("senderEmail")]
        public string SenderEmail { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        public static bool operator ==(GiftCardInfo left, GiftCardInfo right)
            => Equals(left, right);

        public static bool operator !=(GiftCardInfo left, GiftCardInfo right)
            => !Equals(left, right);

        public override int GetHashCode()
            => HashCode.Combine(
                RecipientName?.ToLower(),
                RecipientEmail?.ToLower(),
                SenderName?.ToLower(),
                SenderEmail?.ToLower(),
                Message?.ToLower());

        public override bool Equals(object obj)
        {
            return Equals(obj as GiftCardInfo);
        }

        protected virtual bool Equals(GiftCardInfo other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (RecipientName.EqualsNoCase(other.RecipientName) &&
                RecipientEmail.EqualsNoCase(other.RecipientEmail) &&
                SenderName.EqualsNoCase(other.SenderName) &&
                SenderEmail.EqualsNoCase(other.SenderEmail) &&
                Message.EqualsNoCase(other.Message))
            {
                return true;
            }

            return false;
        }
    }
}