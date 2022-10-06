using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace Smartstore.Core.Localization
{
    // TODO: (core) Make LocalizedValue cacheable/serializable somehow.

    public abstract class LocalizedValue
    {
        // Regex for all types of brackets which need to be "swapped": ({[]})
        private readonly static Regex _rgBrackets = new(@"\(|\{|\[|\]|\}|\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly Language _requestLanguage;
        private readonly Language _currentLanguage;

        protected LocalizedValue(Language requestLanguage, Language currentLanguage)
        {
            _requestLanguage = requestLanguage;
            _currentLanguage = currentLanguage;
        }

        [IgnoreDataMember]
        public Language RequestLanguage => _requestLanguage;

        [IgnoreDataMember]
        public Language CurrentLanguage => _currentLanguage;

        public bool IsFallback => _requestLanguage != _currentLanguage;

        public bool BidiOverride => _requestLanguage != _currentLanguage && _requestLanguage?.Rtl != _currentLanguage?.Rtl;

        /// <summary>
        /// Fixes the flow of brackets within a text if the current page language has RTL flow.
        /// </summary>
        /// <param name="str">The test to fix.</param>
        /// <param name="currentLanguage">Current language</param>
        /// <returns></returns>
        public static string FixBrackets(string str, Language currentLanguage)
        {
            if (!currentLanguage.Rtl || str.IsEmpty())
            {
                return str;
            }

            var controlChar = "&lrm;";
            return _rgBrackets.Replace(str, m =>
            {
                return controlChar + m.Value + controlChar;
            });
        }
    }

    public class LocalizedValue<T> : LocalizedValue, IHtmlContent, IEquatable<LocalizedValue<T>>, IComparable, IComparable<LocalizedValue<T>>
    {
        private T _value;

        internal LocalizedValue(T value) : this(value, null, null)
        {
        }

        public LocalizedValue(T value, Language requestLanguage, Language currentLanguage)
            : base(requestLanguage, currentLanguage)
        {
            _value = value;
        }

        public T Value => _value;

        public void ChangeValue(T value)
        {
            _value = value;
        }

        public static implicit operator T(LocalizedValue<T> obj)
        {
            if (obj == null)
            {
                return default;
            }

            return obj.Value;
        }

        public string ToHtmlString()
            => ToString();

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            => writer.Write(ToString());

        public override string ToString()
        {
            if (_value == null)
            {
                return null;
            }

            if (typeof(T) == typeof(string))
            {
                return _value as string;
            }

            return _value.Convert<string>(CultureInfo.GetCultureInfo(CurrentLanguage?.LanguageCulture ?? "en-US"));
        }

        public override int GetHashCode()
        {
            return _value?.GetHashCode() ?? 0;
        }

        public override bool Equals(object other)
        {
            return Equals(other as LocalizedValue<T>);
        }

        public bool Equals(LocalizedValue<T> other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Equals(_value, other._value);
        }

        public int CompareTo(object other)
        {
            return CompareTo(other as LocalizedValue<T>);
        }

        public int CompareTo(LocalizedValue<T> other)
        {
            if (other == null)
            {
                return 1;
            }

            if (Value is IComparable<T> val1)
            {
                return val1.CompareTo(other.Value);
            }

            if (Value is IComparable val2)
            {
                return val2.CompareTo(other.Value);
            }

            return 0;
        }
    }
}