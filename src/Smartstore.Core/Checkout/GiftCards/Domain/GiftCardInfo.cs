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

        public GiftCardInfo()
        {
        }

        public GiftCardInfo(List<string> list)
        {
            FromList(list);
        }

        // Converter???
        public List<string> ToList()
        {
            return new List<string> {
                RecipientName ?? string.Empty,
                RecipientEmail ?? string.Empty,
                SenderName ?? string.Empty,
                SenderEmail ?? string.Empty,
                Message ?? string.Empty
            };
        }

        public void FromList(List<string> list)
        {
            Guard.NotNull(list, nameof(list));

            if (list.Count is 4 or 5)
            {
                RecipientName = list[0];
                RecipientEmail = list[1];
                SenderName = list[2];
                SenderEmail = list[3];
                Message = list.Count == 5 ? list[4] : string.Empty;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(list), list.Count + " is outside the valid range");
            }
        }

        public void AddAttribute(string name, string value)
        {
            var property = GetType().GetProperty(name);
            if (property is null)
            {
                throw new NullReferenceException("Could not find property of " + nameof(GiftCardInfo) + " with name " + name);
            }

            property.SetValue(this, value);
        }
        
        public bool IsValidInfo()
            => RecipientName.HasValue() && RecipientEmail.HasValue() && SenderName.HasValue() && SenderEmail.HasValue();
    }
}