using System;
using System.Collections.Generic;

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
    }
}