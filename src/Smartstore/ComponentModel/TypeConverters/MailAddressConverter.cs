using System;
using System.Globalization;
using Smartstore.Net.Mail;

namespace Smartstore.ComponentModel.TypeConverters
{
    internal class MailAddressConverter : DefaultTypeConverter
    {
        public MailAddressConverter() : base(typeof(object))
        {
        }

        public override bool CanConvertFrom(Type type)
        {
            return type == typeof(string);
        }

        public override bool CanConvertTo(Type type)
        {
            return type == typeof(string);
        }

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is string str && str.HasValue())
            {
                return new MailAddress(str);
            }

            return base.ConvertFrom(culture, value);
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            if (to == typeof(string) && value is MailAddress address)
            {
                return address.ToString();
            }

            return base.ConvertTo(culture, format, value, to);
        }
    }
}