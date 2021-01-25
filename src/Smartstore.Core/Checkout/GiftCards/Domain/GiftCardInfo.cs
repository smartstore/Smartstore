using System;
using Smartstore.Core.Checkout.Payment;

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

        public static bool operator ==(GiftCardInfo firstCard, GiftCardInfo otherCard)
        {
            return firstCard.RecipientName == otherCard.RecipientName
                && firstCard.RecipientEmail == otherCard.RecipientEmail
                && firstCard.SenderName == otherCard.SenderName
                && firstCard.SenderEmail == otherCard.SenderEmail
                && firstCard.Message == otherCard.Message;
        }

        public static bool operator !=(GiftCardInfo firstCard, GiftCardInfo otherCard)
        {
            return firstCard.RecipientName != otherCard.RecipientName
                || firstCard.RecipientEmail != otherCard.RecipientEmail
                || firstCard.SenderName != otherCard.SenderName
                || firstCard.SenderEmail != otherCard.SenderEmail
                || firstCard.Message != otherCard.Message;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if(obj is GiftCardInfo giftCardInfo)
            {
                return giftCardInfo == this;
            }

            return false;
        }

        public override int GetHashCode() 
            => HashCode.Combine(RecipientName, RecipientEmail, SenderName, SenderEmail, Message);
    }
}