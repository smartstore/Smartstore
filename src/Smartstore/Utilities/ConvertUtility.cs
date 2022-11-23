#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.ComponentModel;

namespace Smartstore.Utilities
{
    public static class ConvertUtility
    {
        public static bool TryConvert<T>(object? value, [MaybeNullWhen(false)] out T? convertedValue)
        {
            convertedValue = default;

            if (TryConvert(value, typeof(T), CultureInfo.InvariantCulture, out object? result))
            {
                convertedValue = (T)result!;
                return true;
            }

            return false;
        }

        public static bool TryConvert<T>(object? value, CultureInfo? culture, [MaybeNullWhen(false)] out T? convertedValue)
        {
            convertedValue = default;

            if (TryConvert(value, typeof(T), culture, out object? result))
            {
                convertedValue = (T)result!;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryConvert(object? value, Type to, [MaybeNullWhen(false)] out object? convertedValue)
        {
            return TryConvert(value, to, CultureInfo.InvariantCulture, out convertedValue);
        }

        public static bool TryConvert(object? value, Type to, CultureInfo? culture, [MaybeNullWhen(false)] out object? convertedValue)
        {
            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            convertedValue = null;

            if (value == null || value == DBNull.Value)
            {
                return to == typeof(string) || to.IsBasicType() == false;
            }

            if (to != typeof(object) && to.IsInstanceOfType(value))
            {
                convertedValue = value;
                return true;
            }

            Type from = value.GetType();

            if (culture == null)
            {
                culture = CultureInfo.InvariantCulture;
            }

            try
            {
                // Get a converter for 'to' (value -> to)
                var converter = TypeConverterFactory.GetConverter(to);
                if (converter != null && converter.CanConvertFrom(from))
                {
                    convertedValue = converter.ConvertFrom(culture, value);
                    return true;
                }

                // Try the other way round with a 'from' converter (to <- from)
                converter = TypeConverterFactory.GetConverter(from);
                if (converter != null && converter.CanConvertTo(to))
                {
                    convertedValue = converter.ConvertTo(culture, null, value, to);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public static ExpandoObject ToExpando(object value)
        {
            Guard.NotNull(value, nameof(value));

            var anonymousDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(value);
            IDictionary<string, object?> expando = new ExpandoObject();
            foreach (var item in anonymousDictionary)
            {
                expando.Add(item);
            }

            return (ExpandoObject)expando;
        }

        public static IDictionary<string, object> ObjectToDictionary(object obj)
        {
            return FastProperty.ObjectToDictionary(
                obj,
                key => key.Replace('_', '-').Replace("@", ""));
        }

        public static IDictionary<string, string?> ObjectToStringDictionary(object obj)
        {
            return ObjectToDictionary(obj)
                .ToDictionary(key => key.Key, el => el.Value.ToString());
        }
    }
}
