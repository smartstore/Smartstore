using System;

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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is GiftCardInfo other)
            {
                return RecipientName == other.RecipientName
                    && RecipientEmail == other.RecipientEmail
                    && SenderName == other.SenderName
                    && SenderEmail == other.SenderEmail
                    && Message == other.Message;
            }

            return false;
        }

        public override int GetHashCode() 
            => HashCode.Combine(RecipientName, RecipientEmail, SenderName, SenderEmail, Message);
    }
}