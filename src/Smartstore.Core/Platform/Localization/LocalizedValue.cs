#nullable enable

using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Newtonsoft.Json;

namespace Smartstore.Core.Localization
{
    public abstract class LocalizedValue
    {
        // Regex for all types of brackets which need to be "swapped": ({[]})
        private readonly static Regex _rgBrackets = new(@"\(|\{|\[|\]|\}|\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        protected LocalizedValue(ILanguage? requestLanguage, ILanguage? currentLanguage)
        {
            RequestLanguage = ToLanguageInfo(requestLanguage);
            CurrentLanguage = ToLanguageInfo(currentLanguage);
        }

        protected static LanguageInfo? ToLanguageInfo(ILanguage? language)
        {
            if (language is null) 
            {
                return null;
            }
            
            if (language is LanguageInfo info) 
            {
                return info;
            }

            return new LanguageInfo(language);
        }

        [DataMember, JsonProperty]
        public LanguageInfo? RequestLanguage { get; private set; }

        [DataMember, JsonProperty]
        public LanguageInfo? CurrentLanguage { get; private set; }

        [IgnoreDataMember]
        public bool IsFallback => RequestLanguage != CurrentLanguage;

        [IgnoreDataMember]
        public bool BidiOverride => RequestLanguage?.Id != CurrentLanguage?.Id && RequestLanguage?.Rtl != CurrentLanguage?.Rtl;

        /// <summary>
        /// Fixes the flow of brackets within a text if the current page language has RTL flow.
        /// </summary>
        /// <param name="str">The test to fix.</param>
        /// <param name="currentLanguage">Current language</param>
        /// <returns></returns>
        public static string? FixBrackets(string? str, ILanguage currentLanguage)
        {
            if (!currentLanguage.Rtl || str!.IsEmpty())
            {
                return str;
            }

            var controlChar = "&lrm;";
            return _rgBrackets.Replace(str!, m =>
            {
                return controlChar + m.Value + controlChar;
            });
        }
    }

    public class LocalizedValue<T> : LocalizedValue, IHtmlContent, IEquatable<LocalizedValue<T>>, IComparable, IComparable<LocalizedValue<T>>
    {
        [JsonConstructor]
        internal LocalizedValue()
            : base(null, null)
        {
            // For serialization
        }

        internal LocalizedValue(T value) 
            : base(null, null)
        {
            Value = value;
        }

        public LocalizedValue(T? value, ILanguage? requestLanguage, ILanguage? currentLanguage)
            : base(requestLanguage, currentLanguage)
        {
            Value = value;
        }

        [DataMember, JsonProperty]
        public T? Value { get; private set; }

        public void ChangeValue(T? value)
        {
            Value = value;
        }

        public static implicit operator T?(LocalizedValue<T> obj)
        {
            if (obj == null)
            {
                return default;
            }

            return obj.Value;
        }

        public string? ToHtmlString()
            => ToString();

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            => writer.Write(ToString());

        public override string? ToString()
        {
            if (Value == null)
            {
                return null;
            }

            if (typeof(T) == typeof(string))
            {
                return Value as string;
            }

            return Value.Convert<string>(CultureInfo.GetCultureInfo(CurrentLanguage?.LanguageCulture ?? "en-US"));
        }

        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }

        public override bool Equals(object? other)
        {
            return Equals(other as LocalizedValue<T>);
        }

        public bool Equals(LocalizedValue<T>? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Equals(Value, other.Value);
        }

        public int CompareTo(object? other)
        {
            return CompareTo(other as LocalizedValue<T>);
        }

        public int CompareTo(LocalizedValue<T>? other)
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