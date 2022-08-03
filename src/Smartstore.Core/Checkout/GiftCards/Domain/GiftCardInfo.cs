namespace Smartstore.Core.Checkout.GiftCards
{
    public interface IGiftCardInfo
    {
        string RecipientName { get; }
        string RecipientEmail { get; }
        string SenderName { get; }
        string SenderEmail { get; }
        string Message { get; }
    }

    public class GiftCardInfo : IGiftCardInfo
    {
        public string RecipientName { get; set; }
        public string RecipientEmail { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
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